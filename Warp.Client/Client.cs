using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using STUN.Enums;
using STUN.StunResult;
using System.Net;
using System.Net.Sockets;
using Warp.Client.Handlers;
using Warp.Client.Helpers;
using Warp.Client.Interfaces;
using Warp.Client.Managers;
using Warp.Client.Models;
using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Packets;
using Wrap.Shared.Network.Packets.Client;
using Wrap.Shared.Network.Packets.Server;

namespace Warp.Client;

public sealed class Client : IClient, IDisposable {
    private ILogger _logger;
    private UserInfo? _userInfo;
    private IEventLoopGroup? _eventLoopGroup;
    internal IChannel? _clientChannel;
    private volatile bool _isConnected = false;
    private volatile bool _disposed = false;
    private volatile bool _isLoggedIn = false;
    private UserProfile _profile = UserProfile.Load();
    private ProxyManager? _proxyManager;
    private LocalProxyServer? _localProxyServer;
    public RoomInfoPacket? CurrentRoomInfo { get; private set; }
    public event EventHandler<RoomInfoPacket>? RoomInfoReceived;
    public event EventHandler<RoomOwnerChangedPacket>? RoomOwnerChanged;
    public event EventHandler<RoomDismissedPacket>? RoomDismissed;
    public event EventHandler<RoomInfoQueryResultPacket>? RoomInfoQueryResultReceived;
    public RoomOwnerChangedPacket? LastRoomOwnerChanged { get; private set; }
    public RoomDismissedPacket? LastRoomDismissed { get; private set; }
    public RoomInfoQueryResultPacket? LastRoomInfoQueryResult { get; private set; }
    public event EventHandler<RoomJoinRequestNoticePacket>? RoomJoinRequestNoticeReceived;
    public event EventHandler<RoomJoinResultPacket>? RoomJoinResultReceived;
    public RoomJoinRequestNoticePacket? LastRoomJoinRequestNotice { get; private set; }
    public RoomJoinResultPacket? LastRoomJoinResult { get; private set; }
    public event EventHandler<RoomJoinResultPacket>? RoomKickResultReceived;
    private readonly List<string> _pendingJoinUserIds = new();
    public IReadOnlyList<string> PendingJoinUserIds => _pendingJoinUserIds.AsReadOnly();
    public event EventHandler<RoomChatMessagePacket>? RoomChatMessageReceived;
    public event EventHandler<UserInfoResultPacket>? UserInfoResultReceived;
    public UserInfoResultPacket? LastUserInfoResult { get; private set; }
    public event EventHandler<KeepAlivePacket>? KeepAliveReceived;

    // P2P连接相关事件
    public event EventHandler<PeerConnectRequestNoticePacket>? PeerConnectRequestReceived;
    public event EventHandler<PeerConnectAcceptNoticePacket>? PeerConnectAcceptReceived;
    public event EventHandler<PeerConnectRejectNoticePacket>? PeerConnectRejectReceived;
    public event EventHandler<PeerIPInfoPacket>? PeerIPInfoReceived;
    public event EventHandler<PeerConnectSuccessPacket>? PeerConnectSuccessReceived;
    public event EventHandler<PeerConnectFailedNoticePacket>? PeerConnectFailedReceived;

    // 新增P2P数据转发和连接相关事件
    public event EventHandler<(string targetUserId, IPeerBoundPacket packet)>? PeerDataSent;
    public event EventHandler<(string sourceUserId, IPeerBoundPacket packet)>? PeerDataReceived;
    public event EventHandler<string>? PeerConnectionEstablished;
    public event EventHandler<string>? PeerConnectionClosed;
    public event EventHandler<(string targetUserId, string reason)>? PeerConnectionFailed;

    // NAT类型检测相关
    public NatType CurrentNatType { get; private set; } = NatType.Unknown;
    public IPEndPoint? PublicEndPoint { get; private set; }
    public IUPnPService? UPnPService { get; set; }
    public event EventHandler<NatType>? NatTypeDetected;
    public event EventHandler<NatType>? NatTypeDetectedEvent;
    public event EventHandler<ProxyManager>? ProxyManagerInitialized;
    public event EventHandler? ProxyForwardingDisabled;

    public bool Disposed => _disposed;
    public bool IsConnected => _isConnected && _clientChannel?.Active == true;
    public bool IsLoggedIn => _isLoggedIn;

    public string? UserId => _userInfo?.UserId;
    public string? Name => _userInfo?.Name ?? _profile.Name;
    public string? DisplayName => _userInfo?.DisplayName ?? _profile.DisplayName;
    public UserProfile Profile => _profile;
    public PeerConnectionManager PeerConnectionManager { get; private set; }
    public ProxyManager? ProxyManager => _proxyManager;
    public LocalProxyServer? LocalProxyServer => _localProxyServer;

    public event EventHandler? Connected;
    public event EventHandler<string>? Disconnected;
    public event EventHandler<UnsolvedPacket>? DataReceived;
    public event EventHandler<IClientBoundPacket>? PacketReceived;
    public event EventHandler<UserInfo>? LoggedIn;
    public event EventHandler<PluginMessagePacket>? PluginMessageReceived;
    public event EventHandler<string>? ServerMessageReceived;

    public IPEndPoint? RemoteIP { get; set; }

    public ILogger Logger => _logger;

    /// <summary>
    /// 无参数构造函数
    /// </summary>
    public Client(ILogger<Client> logger) {
        _logger = logger;
        PeerConnectionManager = new(this);
    }

    /// <summary>
    /// 带用户信息的构造函数
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    public Client(UserInfo userInfo) {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        PeerConnectionManager = new PeerConnectionManager(this);
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    public void SetUserInfo(UserInfo userInfo) {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <returns>用户信息</returns>
    public UserInfo? GetUserInfo() {
        return _userInfo;
    }

    public async Task ConnectAsync(string serverAddress, int port = 10270) {
        CheckDisposed();

        if (_isConnected) {
            throw new InvalidOperationException("客户端已经连接");
        }

        try {
            // 在连接前检测NAT类型
            await DetectNatTypeAsync();

            // 解析服务器地址，确保使用IPv4
            IPAddress? ipAddress = null;
            if (serverAddress.ToLower() == "localhost") {
                ipAddress = IPAddress.Loopback;
            } else if (!IPAddress.TryParse(serverAddress, out ipAddress)) {
                var hostEntry = await Dns.GetHostEntryAsync(serverAddress);
                // 强制选择IPv4地址
                ipAddress = hostEntry.AddressList.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);

                if (ipAddress == null) {
                    throw new ArgumentException($"无法解析服务器地址为IPv4: {serverAddress}");
                }
            }

            // 确保使用IPv4地址
            if (ipAddress.AddressFamily != AddressFamily.InterNetwork) {
                throw new ArgumentException($"服务器地址必须是IPv4地址: {serverAddress} (当前: {ipAddress.AddressFamily})");
            }

            _eventLoopGroup = new MultithreadEventLoopGroup(1);

            var bootstrap = new Bootstrap();
            bootstrap.Group(_eventLoopGroup)
                   .ChannelFactory(() => new TcpSocketChannel(AddressFamily.InterNetwork))
                   .Option(ChannelOption.SoKeepalive, true)
                   .Option(ChannelOption.TcpNodelay, true)
                   .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(10)) // 添加连接超时
                   .Handler(new ActionChannelInitializer<ISocketChannel>(channel => {
                       var pipeline = channel.Pipeline;

                       pipeline.AddLast(new LoggingHandler("Client"));
                       pipeline.AddLast(new LengthFieldBasedFrameDecoder(65536, 0, 4, 0, 4));
                       pipeline.AddLast(new LengthFieldPrepender(4));
                       pipeline.AddLast(new ClientHandler(this));
                   }));

            // 连接到服务器
            var endpoint = new IPEndPoint(ipAddress, port);
            _clientChannel = await bootstrap.ConnectAsync(endpoint);
            _isConnected = true;

            Connected?.Invoke(this, EventArgs.Empty);
        } catch (Exception) {
            await DisconnectAsync();
            throw;
        }
    }

    public async Task DisconnectAsync() {
        CheckDisposed();

        if (!_isConnected)
            return;

        try {
            if (RemoteIP != null)
                _ = UPnPService?.DeletePortMappingAsync(RemoteIP.Port, SocketProtocol.TCP);
            _isConnected = false;
            _isLoggedIn = false;

            if (_clientChannel != null) {
                await _clientChannel.CloseAsync();
                _clientChannel = null;
            }

            if (_eventLoopGroup != null) {
                _ = _eventLoopGroup.ShutdownGracefullyAsync();
                _eventLoopGroup = null;
            }

            Disconnected?.Invoke(this, "");
        } catch (Exception) { }
    }

    public async Task SendPacketAsync(IServerBoundPacket packet) {
        CheckDisposed();

        if (!IsConnected) {
            throw new InvalidOperationException("客户端未连接");
        }

        if (packet == null) {
            throw new ArgumentNullException(nameof(packet));
        }

        try {
            var serializer = packet.GetSerializer();
            var data = serializer.Serialize(packet);
            packet.OnSerialize(ref data);

            if (_clientChannel != null) {
                // 创建包含数据包类型和数据的完整数据包
                var packetData = new byte[4 + data.Length];
                BitConverter.GetBytes((int)packet.GetPacketType()).CopyTo(packetData, 0);
                data.CopyTo(packetData, 4);

                var buffer = Unpooled.WrappedBuffer(packetData);
                await _clientChannel.WriteAndFlushAsync(buffer);
            }
        } catch (Exception) {
            throw;
        }
    }

    /// <summary>
    /// 发送登录数据包
    /// </summary>
    public async Task SendLoginPacketAsync() {
        if (_userInfo == null) {
            throw new InvalidOperationException("用户信息未设置，无法发送登录数据包");
        }

        var loginPacket = new LoginPacket(_userInfo);
        await SendPacketAsync(loginPacket);
    }

    /// <summary>
    /// 发送登录数据包（公开方法）
    /// </summary>
    public async Task SendLoginPacketAsync(UserInfo userInfo) {
        SetUserInfo(userInfo);
        await SendLoginPacketAsync();
    }

    /// <summary>
    /// 处理登录成功
    /// </summary>
    /// <param name="userInfo">服务器返回的用户信息</param>
    internal void OnLoginSuccess(UserInfo userInfo) {
        _userInfo = userInfo;
        _isLoggedIn = true;
        // 自动保存用户名和显示名
        if (!string.IsNullOrWhiteSpace(userInfo.Name)) _profile.Name = userInfo.Name;
        if (!string.IsNullOrWhiteSpace(userInfo.DisplayName)) _profile.DisplayName = userInfo.DisplayName;
        _profile.Save();
        LoggedIn?.Invoke(this, userInfo);

        // 初始化代理功能
        _ = InitializeProxyAsync();
    }

    internal async Task OnDataReceivedAsync(UnsolvedPacket unsolved) {
        DataReceived?.Invoke(this, unsolved);
    }

    internal async Task OnPacketReceivedAsync(IClientBoundPacket packet) {
        PacketReceived?.Invoke(this, packet);
    }

    internal async Task OnDisconnectPacketReceivedAsync(DisconnectPacket packet) {
        _isConnected = false;
        _isLoggedIn = false;
        // 触发断开连接事件
        Disconnected?.Invoke(this, packet.Reason);

        _ = DisconnectAsync();
    }

    internal async Task OnRoomInfoReceivedAsync(RoomInfoPacket packet) {
        CurrentRoomInfo = packet;
        RoomInfoReceived?.Invoke(this, packet);

        // 自动建立P2P连接
        await AutoEstablishP2PConnectionsAsync(packet);

        // 重新初始化代理功能（因为房间信息可能已更新）
        await InitializeProxyAsync();
    }

    internal async Task OnRoomOwnerChangedAsync(RoomOwnerChangedPacket packet) {
        LastRoomOwnerChanged = packet;
        RoomOwnerChanged?.Invoke(this, packet);
    }
    internal async Task OnRoomDismissedAsync(RoomDismissedPacket packet) {
        if (CurrentRoomInfo != null && CurrentRoomInfo.RoomId == packet.RoomId) {
            CurrentRoomInfo = null;
        }
        LastRoomDismissed = packet;
        RoomDismissed?.Invoke(this, packet);
        PeerConnectionManager.CloseAllConnections();
    }
    internal async Task OnRoomInfoQueryResultAsync(RoomInfoQueryResultPacket packet) {
        LastRoomInfoQueryResult = packet;
        RoomInfoQueryResultReceived?.Invoke(this, packet);
    }
    internal async Task OnRoomJoinRequestNoticeAsync(RoomJoinRequestNoticePacket packet) {
        LastRoomJoinRequestNotice = packet;
        if (!string.IsNullOrEmpty(packet.ApplicantUserId) && !_pendingJoinUserIds.Contains(packet.ApplicantUserId)) {
            _pendingJoinUserIds.Add(packet.ApplicantUserId);
        }
        RoomJoinRequestNoticeReceived?.Invoke(this, packet);
    }
    internal async Task OnRoomJoinResultAsync(RoomJoinResultPacket packet) {
        LastRoomJoinResult = packet;
        RoomJoinResultReceived?.Invoke(this, packet);
        // 踢人结果：如果是失败且消息包含“踢出”，则触发踢人结果事件
        if (!packet.Success && packet.Message.Contains("踢出")) {
            RoomKickResultReceived?.Invoke(this, packet);
        }
    }
    internal async Task OnRoomChatMessageReceivedAsync(RoomChatMessagePacket packet) {
        RoomChatMessageReceived?.Invoke(this, packet);
        var time = packet.Timestamp.ToLocalTime().ToString("HH:mm:ss");
    }
    internal async Task OnUserInfoResultReceivedAsync(UserInfoResultPacket packet) {
        LastUserInfoResult = packet;
        UserInfoResultReceived?.Invoke(this, packet);
    }
    internal async Task OnKeepAliveReceivedAsync(KeepAlivePacket packet) {
        KeepAliveReceived?.Invoke(this, packet);
        // 立即回复相同的Value
        await SendPacketAsync(new KeepAliveResponsePacket(packet.Value));
    }

    // P2P连接处理方法
    internal async Task OnPeerConnectRequestReceivedAsync(PeerConnectRequestNoticePacket packet) {
        PeerConnectRequestReceived?.Invoke(this, packet);
    }

    internal async Task OnPeerConnectAcceptReceivedAsync(PeerConnectAcceptNoticePacket packet) {
        PeerConnectAcceptReceived?.Invoke(this, packet);
    }

    internal async Task OnPeerConnectRejectReceivedAsync(PeerConnectRejectNoticePacket packet) {
        PeerConnectRejectReceived?.Invoke(this, packet);
    }

    internal async Task OnPeerIPInfoReceivedAsync(PeerIPInfoPacket packet) {
        PeerIPInfoReceived?.Invoke(this, packet);

        // 实现NAT穿透逻辑
        _ = Task.Run(async () => {
            try {
                await EstablishP2PConnectionAsync(packet.TargetUserId, new IPEndPoint(IPAddress.Parse(string.Join(".", packet.TargetIPAddress)), packet.TargetPort));
            } catch (Exception) {
                await SendPacketAsync(new PeerConnectFailedPacket { TargetUserId = packet.TargetUserId });
            }
        });
    }

    internal async Task OnPeerConnectSuccessReceivedAsync(PeerConnectSuccessPacket packet) {
        PeerConnectSuccessReceived?.Invoke(this, packet);

        // 如果是普通用户且与房主建立了连接，重新初始化代理
        if (CurrentRoomInfo != null && _profile.EnableProxyForwarding) {
            var isOwner = CurrentRoomInfo.Owner.UserId == _userInfo?.UserId;
            if (!isOwner && packet.TargetUserId == CurrentRoomInfo.Owner.UserId) {
                _ = InitializeProxyAsync();
            }
        }
    }

    internal async Task OnPeerConnectFailedReceivedAsync(PeerConnectFailedNoticePacket packet) {
        PeerConnectFailedReceived?.Invoke(this, packet);
    }

    /// <summary>
    /// 自动建立P2P连接
    /// </summary>
    /// <param name="roomInfo">房间信息</param>
    private async Task AutoEstablishP2PConnectionsAsync(RoomInfoPacket roomInfo) {
        if (_userInfo == null) return;

        var currentUserId = _userInfo.UserId;
        var isOwner = roomInfo.Owner.UserId == currentUserId;

        if (isOwner) {
            // 只有房主向所有其他成员发起P2P连接
            foreach (var user in roomInfo.Users) {
                if (user.UserId != currentUserId) {
                    await SendPacketAsync(new PeerConnectRequestPacket(user.UserId));
                }
            }
        }
        // 普通成员不主动发起P2P连接，只等待房主的连接请求
    }
    // 申请加入房间
    public async Task RequestJoinRoomAsync(int roomId) {
        await SendPacketAsync(new RoomJoinRequestPacket(roomId));
    }
    // 房主审批同意用户加入
    public async Task ApproveJoinRoomAsync(int roomId, string userId) {
        await SendPacketAsync(new RoomJoinApprovePacket(roomId, userId));
        _pendingJoinUserIds.Remove(userId);
    }
    // 房主拒绝用户加入
    public async Task RejectJoinRoomAsync(int roomId, string userId) {
        await SendPacketAsync(new RoomJoinRejectPacket(roomId, userId));
        _pendingJoinUserIds.Remove(userId);
    }
    // 房主主动转让房主
    public async Task TransferRoomOwnerAsync(int roomId, string newOwnerUserId) {
        await SendPacketAsync(new RoomTransferOwnerPacket(roomId, newOwnerUserId));
    }
    // 房主主动解散房间
    public async Task DismissRoomAsync(int roomId) {
        await SendPacketAsync(new RoomDismissPacket(roomId));
    }
    // 踢人（房主主动调用）
    public async Task KickUserFromRoomAsync(int roomId, string userId) {
        await SendPacketAsync(new RoomKickPacket(roomId, userId));
    }
    // 查询用户信息
    public async Task QueryUserInfoAsync(string userId) {
        await SendPacketAsync(new UserInfoQueryPacket(userId));
    }

    /// <summary>
    /// 发送P2P心跳包
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    public async Task SendPeerKeepAliveAsync(string targetUserId) {
        if (PeerConnectionManager.HasConnection(targetUserId)) {
            await PeerConnectionManager.SendKeepAliveAsync(targetUserId);
        }
    }

    /// <summary>
    /// 发送P2P心跳包（同步版本）
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    public void SendPeerKeepAlive(string targetUserId) {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        SendPeerKeepAliveAsync(targetUserId).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    /// <summary>
    /// 获取所有P2P连接
    /// </summary>
    /// <returns>活跃的P2P连接列表</returns>
    public IEnumerable<string> GetPeerConnections() {
        return PeerConnectionManager.GetActiveConnections();
    }

    /// <summary>
    /// 检查是否有P2P连接
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <returns>是否有连接</returns>
    public bool HasPeerConnection(string targetUserId) {
        return PeerConnectionManager.HasConnection(targetUserId);
    }

    /// <summary>
    /// 设置P2P心跳间隔
    /// </summary>
    /// <param name="intervalSeconds">心跳间隔（秒）</param>
    public void SetPeerHeartbeatInterval(int intervalSeconds) {
        PeerConnectionManager.SetHeartbeatInterval(intervalSeconds);
    }

    /// <summary>
    /// 获取P2P心跳状态
    /// </summary>
    /// <returns>心跳状态信息</returns>
    public Dictionary<string, object> GetPeerKeepAliveStatus() {
        return PeerConnectionManager.GetKeepAliveStatus();
    }

    /// <summary>
    /// 关闭与指定用户的P2P连接
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    public void ClosePeerConnection(string targetUserId) {
        PeerConnectionManager.CloseConnection(targetUserId);
    }

    /// <summary>
    /// 关闭所有P2P连接
    /// </summary>
    public void CloseAllPeerConnections() {
        PeerConnectionManager.CloseAllConnections();
    }

    /// <summary>
    /// 获取与指定用户的P2P连接状态
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <returns>连接状态</returns>
    public ConnectionStatus GetPeerConnectionStatus(string targetUserId) {
        return PeerConnectionManager.GetConnectionStatus(targetUserId);
    }

    /// <summary>
    /// 发送P2P数据包
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="packet">数据包</param>
    public async Task SendPeerPacketAsync(string targetUserId, IPeerBoundPacket packet) {
        // 发送前触发事件
        PeerDataSent?.Invoke(this, (targetUserId, packet));
        await PeerConnectionManager.SendPacketAsync(targetUserId, packet);
        // 发送后可选触发（如需区分发送成功/失败可扩展）
    }

    /// <summary>
    /// 获取综合代理统计信息
    /// </summary>
    /// <returns>综合代理统计信息</returns>
    public ProxyStatistics GetProxyStatistics() {
        return new ProxyStatistics {
            LocalProxyStats = _localProxyServer?.GetLocalProxyStatistics(),
            ProxyManagerStats = _proxyManager?.GetProxyStatistics()
        };
    }

    private void CheckDisposed() {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// 初始化代理功能
    /// </summary>
    private async Task InitializeProxyAsync() {
        try {
            if (!_profile.EnableProxyForwarding) {
                ProxyForwardingDisabled?.Invoke(this, EventArgs.Empty);
                return;
            }

            // 等待一段时间让P2P连接建立
            await Task.Delay(2000);

            // 根据用户角色初始化不同的代理功能
            if (CurrentRoomInfo != null) {
                var isOwner = CurrentRoomInfo.Owner.UserId == _userInfo?.UserId;

                if (isOwner) {
                    // 房主：初始化代理管理器
                    _proxyManager = new ProxyManager(this);
                    ProxyManagerInitialized?.Invoke(this, _proxyManager);
                } else {
                    // 检查是否有到房主的P2P连接
                    var ownerUserId = CurrentRoomInfo.Owner.UserId;
                    if (PeerConnectionManager.HasConnection(ownerUserId)) {
                        // 普通用户：初始化本地代理服务器
                        _localProxyServer = new LocalProxyServer(this, _profile.LocalProxyPort,
                            _profile.ProxyTargetAddress, _profile.ProxyTargetPort);

                        await _localProxyServer.StartAsync();
                    }
                }
            }
        } catch (Exception) {

        }
    }

    /// <summary>
    /// 检测NAT类型
    /// </summary>
    private async Task DetectNatTypeAsync() {
        try {
            ClassicStunResult result;

            // 尝试使用UPnP服务
            if (UPnPService != null) {
                IPEndPoint endPoint = new(new IPAddress(new byte[] { 0, 0, 0, 0 }), Random.Shared.Next(1000, 65535));
                await UPnPService.AddPortMappingAsync(endPoint.Port, SocketProtocol.UDP, endPoint.Port, "Wrap NAT test");
                result = await StunHelper.GetClassicStunResultAsync(endPoint);
                await UPnPService.DeletePortMappingAsync(endPoint.Port, SocketProtocol.UDP);
            } else
                result = await StunHelper.GetClassicStunResultAsync();

            CurrentNatType = result.NatType;
            PublicEndPoint = result.PublicEndPoint;

            NatTypeDetectedEvent?.Invoke(this, CurrentNatType);
        } catch (Exception ex) {
            CurrentNatType = NatType.Unknown;
        }
    }

    /// <summary>
    /// 建立P2P连接
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="peerEndPoint">对等端端点</param>
    private async Task EstablishP2PConnectionAsync(string targetUserId, IPEndPoint peerEndPoint) {
        var client = new TcpClient();
        client.Client.ExclusiveAddressUse = false;
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.Client.Bind(_clientChannel!.LocalAddress);

        TcpClient? connectionToPeer = null;
        bool connectionEstablished = false;
        object connectLock = new object();

        // 主动连接任务
        async Task ConnectToPeerAsync() {
            for (int i = 0; i < 8; i++) {
                try {
                    await client.ConnectAsync(peerEndPoint);
                    lock (connectLock) {
                        if (!connectionEstablished) {
                            connectionToPeer = client;
                            connectionEstablished = true;
                        } else {
                            client.Close();
                        }
                    }
                    break;
                } catch (Exception ex) {
                    if (i < 3) {
                        await Task.Delay(1000);
                    }
                }
            }
        }

        // 被动监听任务
        TcpListener? listener = null;
        async Task ListenForPeerAsync() {
            try {
                IPEndPoint endPoint = (IPEndPoint)_clientChannel!.LocalAddress;
                listener = new TcpListener(endPoint.Address, endPoint.Port);
                listener.Server.ExclusiveAddressUse = false;
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Start();
                while (true) {
                    var peer = await listener.AcceptTcpClientAsync();
                    lock (connectLock) {
                        if (!connectionEstablished) {
                            connectionToPeer = peer;
                            connectionEstablished = true;
                        } else {
                            peer.Close();
                        }
                    }
                    if (connectionEstablished) break;
                }
            } catch (Exception) {

            }
        }

        // 并发执行连接和监听
        var connectTask = ConnectToPeerAsync();
        var listenTask = ListenForPeerAsync();
        await Task.WhenAny(connectTask, listenTask);

        // 等待第一个连接建立
        while (!connectionEstablished) {
            await Task.Delay(50);
        }
        // 关闭监听器
        listener?.Stop();

        if (connectionToPeer == null) {
            client.Close();
            await SendPacketAsync(new PeerConnectFailedPacket(
                targetUserId,
                "无法建立P2P连接",
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            ));
            // 触发连接失败事件
            PeerConnectionFailed?.Invoke(this, (targetUserId, "无法建立P2P连接"));
            return;
        }

        try {
            PeerConnectionManager.AddConnection(targetUserId, connectionToPeer);
            // 触发连接建立事件
            PeerConnectionEstablished?.Invoke(this, targetUserId);
        } catch (Exception ex) {
            PeerConnectionFailed?.Invoke(this, (targetUserId, ex.Message));
        }
    }

    public void Dispose() {
        if (_disposed) return;

        _disposed = true;

        try {
            // 清理代理资源
            _proxyManager?.Dispose();
            _localProxyServer?.Dispose();

            // 释放P2P连接管理器
            PeerConnectionManager?.Dispose();

            // 异步断开连接，但不等待完成
            _ = DisconnectAsync();
        } catch (Exception) {
            // 忽略断开连接时的错误
        }
    }

    internal async Task OnPluginMessageReceivedAsync(PluginMessagePacket pluginMessagePacket) {
        PluginMessageReceived?.Invoke(this, pluginMessagePacket);
    }

    internal async Task OnServerMessageReceivedAsync(ServerMessagePacket serverMessagePacket) {
        ServerMessageReceived?.Invoke(this, serverMessagePacket.Message);
    }
}