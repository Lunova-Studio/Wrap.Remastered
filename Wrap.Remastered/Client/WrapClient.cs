using ConsoleInteractive;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using STUN.Enums;
using STUN.StunResult;
using System.Net;
using System.Net.Sockets;
using Wrap.Remastered.Helpers;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Network.Protocol.PeerBound;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Schemas;
using static Wrap.Remastered.Interfaces.IUPnPService;

namespace Wrap.Remastered.Client;

public class WrapClient : IWrapClient, IDisposable
{
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



    // NAT类型检测相关
    public NatType CurrentNatType { get; private set; } = NatType.Unknown;
    public IPEndPoint? PublicEndPoint { get; private set; }
    public IUPnPService? UPnPService { get; set; }
    public event EventHandler<NatType>? NatTypeDetected;

    public bool Disposed => _disposed;
    public bool IsConnected => _isConnected && _clientChannel?.Active == true;
    public bool IsLoggedIn => _isLoggedIn;

    public string? UserId => _userInfo?.UserId;
    public string? Name => _userInfo?.Name ?? _profile.Name;
    public string? DisplayName => _userInfo?.DisplayName ?? _profile.DisplayName;
    public UserProfile Profile => _profile;
    public ClientCommandManager ClientCommandManager { get; private set; }
    public PeerConnectionManager PeerConnectionManager { get; private set; }
    public ProxyManager? ProxyManager => _proxyManager;
    public LocalProxyServer? LocalProxyServer => _localProxyServer;

    public event EventHandler? Connected;
    public event EventHandler<string>? Disconnected;
    public event EventHandler<UnsolvedPacket>? DataReceived;
    public event EventHandler<IClientBoundPacket>? PacketReceived;
    public event EventHandler<UserInfo>? LoggedIn;

    public IPEndPoint? RemoteIP { get; set; }

    /// <summary>
    /// 无参数构造函数
    /// </summary>
    public WrapClient()
    {
        ClientCommandManager = new ClientCommandManager(this);
        PeerConnectionManager = new PeerConnectionManager(this);
    }

    /// <summary>
    /// 带用户信息的构造函数
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    public WrapClient(UserInfo userInfo)
    {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        ClientCommandManager = new ClientCommandManager(this);
        PeerConnectionManager = new PeerConnectionManager(this);
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    public void SetUserInfo(UserInfo userInfo)
    {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <returns>用户信息</returns>
    public UserInfo? GetUserInfo()
    {
        return _userInfo;
    }

    public async Task ConnectAsync(string serverAddress, int port = 10270)
    {
        CheckDisposed();

        if (_isConnected)
        {
            throw new InvalidOperationException("客户端已经连接");
        }

        try
        {
            // 在连接前检测NAT类型
            await DetectNatTypeAsync();

            // 解析服务器地址，确保使用IPv4
            IPAddress? ipAddress = null;
            if (serverAddress.ToLower() == "localhost")
            {
                ipAddress = IPAddress.Loopback;
            }
            else if (!IPAddress.TryParse(serverAddress, out ipAddress))
            {
                var hostEntry = await Dns.GetHostEntryAsync(serverAddress);
                // 强制选择IPv4地址
                ipAddress = hostEntry.AddressList.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);

                if (ipAddress == null)
                {
                    throw new ArgumentException($"无法解析服务器地址为IPv4: {serverAddress}");
                }
            }

            // 确保使用IPv4地址
            if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                throw new ArgumentException($"服务器地址必须是IPv4地址: {serverAddress} (当前: {ipAddress.AddressFamily})");
            }

            ConsoleWriter.WriteLine($"[连接] 连接到IPv4地址: {ipAddress}:{port}");

            _eventLoopGroup = new MultithreadEventLoopGroup(1);

            var bootstrap = new Bootstrap();
            bootstrap.Group(_eventLoopGroup)
                   .ChannelFactory(() => new TcpSocketChannel(AddressFamily.InterNetwork))
                   .Option(ChannelOption.SoKeepalive, true)
                   .Option(ChannelOption.TcpNodelay, true)
                   .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(10)) // 添加连接超时
                   .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                   {
                       var pipeline = channel.Pipeline;

                       pipeline.AddLast(new LoggingHandler("Client"));
                       pipeline.AddLast(new LengthFieldBasedFrameDecoder(65536, 0, 4, 0, 4));
                       pipeline.AddLast(new LengthFieldPrepender(4));
                       pipeline.AddLast(new ClientHandler(this));
                   }));

            // 连接到服务器
            var endpoint = new IPEndPoint(ipAddress, port);
            ConsoleWriter.WriteLine($"[连接] 尝试连接到: {endpoint}");
            _clientChannel = await bootstrap.ConnectAsync(endpoint);
            _isConnected = true;

            // 验证连接地址
            if (_clientChannel.RemoteAddress is IPEndPoint remoteEndPoint)
            {
                ConsoleWriter.WriteLine($"[连接] 成功连接到: {remoteEndPoint}");
                if (remoteEndPoint.AddressFamily != AddressFamily.InterNetwork)
                {
                    ConsoleWriter.WriteLine($"[连接] 警告: 连接到非IPv4地址: {remoteEndPoint.AddressFamily}");
                }
            }

            Connected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            await DisconnectAsync();
            throw;
        }
    }

    public void Connect(string serverAddress, int port = 10270)
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        ConnectAsync(serverAddress, port).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    public async Task DisconnectAsync()
    {
        CheckDisposed();

        if (!_isConnected)
            return;

        try
        {
            _isConnected = false;
            _isLoggedIn = false;

            if (_clientChannel != null)
            {
                await _clientChannel.CloseAsync();
                _clientChannel = null;
            }

            if (_eventLoopGroup != null)
            {
                await _eventLoopGroup.ShutdownGracefullyAsync();
                _eventLoopGroup = null;
            }

            Disconnected?.Invoke(this, "");
        }
        catch (Exception) { }
    }

    public void Disconnect()
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        DisconnectAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    public async Task SendPacketAsync(IServerBoundPacket packet)
    {
        CheckDisposed();

        if (!IsConnected)
        {
            throw new InvalidOperationException("客户端未连接");
        }

        if (packet == null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        try
        {
            var serializer = packet.GetSerializer();
            var data = serializer.Serialize(packet);
            packet.OnSerialize(ref data);

            if (_clientChannel != null)
            {
                // 创建包含数据包类型和数据的完整数据包
                var packetData = new byte[4 + data.Length];
                BitConverter.GetBytes((int)packet.GetPacketType()).CopyTo(packetData, 0);
                data.CopyTo(packetData, 4);

                var buffer = Unpooled.WrappedBuffer(packetData);
                await _clientChannel.WriteAndFlushAsync(buffer);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public void SendPacket(IServerBoundPacket packet)
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        SendPacketAsync(packet).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    /// <summary>
    /// 发送登录数据包
    /// </summary>
    public async Task SendLoginPacketAsync()
    {
        if (_userInfo == null)
        {
            throw new InvalidOperationException("用户信息未设置，无法发送登录数据包");
        }

        var loginPacket = new LoginPacket(_userInfo);
        await SendPacketAsync(loginPacket);
    }

    /// <summary>
    /// 发送登录数据包（公开方法）
    /// </summary>
    public async Task SendLoginPacketAsync(UserInfo userInfo)
    {
        SetUserInfo(userInfo);
        await SendLoginPacketAsync();
    }

    /// <summary>
    /// 处理登录成功
    /// </summary>
    /// <param name="userInfo">服务器返回的用户信息</param>
    internal void OnLoginSuccess(UserInfo userInfo)
    {
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

    internal void OnDataReceived(UnsolvedPacket unsolved)
    {
        DataReceived?.Invoke(this, unsolved);
    }

    internal void OnPacketReceived(IClientBoundPacket packet)
    {
        PacketReceived?.Invoke(this, packet);
    }

    internal void OnDisconnectPacketReceived(DisconnectPacket packet)
    {
        _isConnected = false;
        _isLoggedIn = false;
        // 触发断开连接事件
        Disconnected?.Invoke(this, packet.Reason);

        _ = DisconnectAsync();
    }

    internal void OnRoomInfoReceived(RoomInfoPacket packet)
    {
        CurrentRoomInfo = packet;
        RoomInfoReceived?.Invoke(this, packet);
        ConsoleWriter.WriteLine($"[房间] 加入房间: {packet.RoomName} (ID: {packet.RoomId})，房主: {packet.Owner.DisplayName}，成员数: {packet.Users.Count}/{packet.MaxUsers}");

        // 自动建立P2P连接
        AutoEstablishP2PConnections(packet);

        // 重新初始化代理功能（因为房间信息可能已更新）
        _ = InitializeProxyAsync();
    }

    internal void OnRoomOwnerChanged(RoomOwnerChangedPacket packet)
    {
        LastRoomOwnerChanged = packet;
        RoomOwnerChanged?.Invoke(this, packet);
        ConsoleWriter.WriteLine($"[房间] 房主已变更，房间ID: {packet.RoomId}，新房主UserId: {packet.NewOwnerUserId}");
    }
    internal void OnRoomDismissed(RoomDismissedPacket packet)
    {
        if (CurrentRoomInfo != null && CurrentRoomInfo.RoomId == packet.RoomId)
        {
            CurrentRoomInfo = null;
        }
        LastRoomDismissed = packet;
        RoomDismissed?.Invoke(this, packet);
        ConsoleWriter.WriteLine($"[房间] 房间已解散，房间ID: {packet.RoomId}");
        PeerConnectionManager.CloseAllConnections();
    }
    internal void OnRoomInfoQueryResult(RoomInfoQueryResultPacket packet)
    {
        LastRoomInfoQueryResult = packet;
        RoomInfoQueryResultReceived?.Invoke(this, packet);
        ConsoleWriter.WriteLine($"[房间] 查询结果：房间ID: {packet.RoomId}，名称: {packet.RoomName}，房主: {packet.OwnerUserId}，人数: {packet.UserCount}/{packet.MaxUsers}");
    }
    internal void OnRoomJoinRequestNotice(RoomJoinRequestNoticePacket packet)
    {
        LastRoomJoinRequestNotice = packet;
        if (!string.IsNullOrEmpty(packet.ApplicantUserId) && !_pendingJoinUserIds.Contains(packet.ApplicantUserId))
        {
            _pendingJoinUserIds.Add(packet.ApplicantUserId);
        }
        RoomJoinRequestNoticeReceived?.Invoke(this, packet);
        ConsoleWriter.WriteLine($"[房间] 有用户申请加入房间，房间ID: {packet.RoomId}，申请者UserId: {packet.ApplicantUserId}");
    }
    internal void OnRoomJoinResult(RoomJoinResultPacket packet)
    {
        LastRoomJoinResult = packet;
        RoomJoinResultReceived?.Invoke(this, packet);
        // 踢人结果：如果是失败且消息包含“踢出”，则触发踢人结果事件
        if (!packet.Success && packet.Message.Contains("踢出"))
        {
            RoomKickResultReceived?.Invoke(this, packet);
            ConsoleWriter.WriteLine($"[房间] 你被房主踢出了房间 (ID: {packet.RoomId})");
        }
        else
        {
            ConsoleWriter.WriteLine($"[房间] 申请加入房间{packet.RoomId}结果: {(packet.Success ? "成功" : "失败")}，消息: {packet.Message}");
        }
    }
    internal void OnRoomChatMessageReceived(RoomChatMessagePacket packet)
    {
        RoomChatMessageReceived?.Invoke(this, packet);
        var time = packet.Timestamp.ToLocalTime().ToString("HH:mm:ss");
        ConsoleWriter.WriteLineFormatted($"§b[{time}] §a{packet.SenderDisplayName}§f: {packet.Message}");
    }
    internal void OnUserInfoResultReceived(UserInfoResultPacket packet)
    {
        LastUserInfoResult = packet;
        UserInfoResultReceived?.Invoke(this, packet);
        ConsoleWriter.WriteLineFormatted($"§a用户信息: UserId={packet.UserInfo.UserId}, Name={packet.UserInfo.Name}, DisplayName={packet.UserInfo.DisplayName}");
    }
    internal void OnKeepAliveReceived(KeepAlivePacket packet)
    {
        KeepAliveReceived?.Invoke(this, packet);
        // 立即回复相同的Value
        SendPacket(new KeepAliveResponsePacket(packet.Value));
    }

    // P2P连接处理方法
    internal void OnPeerConnectRequestReceived(PeerConnectRequestNoticePacket packet)
    {
        PeerConnectRequestReceived?.Invoke(this, packet);
        ConsoleWriter.WriteLine($"[P2P] 收到来自 {packet.RequesterDisplayName} 的P2P连接请求");
    }

    internal void OnPeerConnectAcceptReceived(PeerConnectAcceptNoticePacket packet)
    {
        PeerConnectAcceptReceived?.Invoke(this, packet);
        ConsoleWriter.WriteLine($"[P2P] {packet.AccepterDisplayName} 接受了你的P2P连接请求");
    }

    internal void OnPeerConnectRejectReceived(PeerConnectRejectNoticePacket packet)
    {
        PeerConnectRejectReceived?.Invoke(this, packet);
        ConsoleWriter.WriteLine($"[P2P] {packet.RejecterDisplayName} 拒绝了你的P2P连接请求，原因: {packet.Reason}");
    }

    internal void OnPeerIPInfoReceived(PeerIPInfoPacket packet)
    {
        PeerIPInfoReceived?.Invoke(this, packet);
        ConsoleWriter.WriteLine($"[P2P] 收到 {packet.TargetUserId} 的IP信息，准备建立P2P连接");

        // 实现NAT穿透逻辑
        _ = Task.Run(async () =>
        {
            try
            {
                await EstablishP2PConnectionAsync(packet.TargetUserId, new IPEndPoint(IPAddress.Parse(string.Join(".", packet.TargetIPAddress)), packet.TargetPort));
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteLine($"[P2P] 建立连接失败: {ex.Message}");
                await SendPacketAsync(new PeerConnectFailedPacket { TargetUserId = packet.TargetUserId });
            }
        });
    }

    internal void OnPeerConnectSuccessReceived(PeerConnectSuccessPacket packet)
    {
        PeerConnectSuccessReceived?.Invoke(this, packet);
        ConsoleWriter.WriteLine($"[P2P] 与 {packet.TargetUserId} 的P2P连接建立成功");

        // 如果是普通用户且与房主建立了连接，重新初始化代理
        if (CurrentRoomInfo != null && _profile.EnableProxyForwarding)
        {
            var isOwner = CurrentRoomInfo.Owner.UserId == _userInfo?.UserId;
            ConsoleWriter.WriteLine($"[代理] 调试信息: isOwner={isOwner}, TargetUserId={packet.TargetUserId}, OwnerUserId={CurrentRoomInfo.Owner.UserId}");
            if (!isOwner && packet.TargetUserId == CurrentRoomInfo.Owner.UserId)
            {
                ConsoleWriter.WriteLine("[代理] 与房主P2P连接建立，重新初始化代理功能");
                _ = InitializeProxyAsync();
            }
        }
        else
        {
            ConsoleWriter.WriteLine($"[代理] 调试信息: CurrentRoomInfo={CurrentRoomInfo != null}, EnableProxyForwarding={_profile.EnableProxyForwarding}");
        }
    }

    internal void OnPeerConnectFailedReceived(PeerConnectFailedNoticePacket packet)
    {
        PeerConnectFailedReceived?.Invoke(this, packet);
        ConsoleWriter.WriteLine($"[P2P] 与 {packet.TargetUserId} 的P2P连接失败，原因: {packet.Reason}");
    }

    /// <summary>
    /// 自动建立P2P连接
    /// </summary>
    /// <param name="roomInfo">房间信息</param>
    private void AutoEstablishP2PConnections(RoomInfoPacket roomInfo)
    {
        if (_userInfo == null) return;

        var currentUserId = _userInfo.UserId;
        var isOwner = roomInfo.Owner.UserId == currentUserId;

        if (isOwner)
        {
            // 只有房主向所有其他成员发起P2P连接
            foreach (var user in roomInfo.Users)
            {
                if (user.UserId != currentUserId)
                {
                    ConsoleWriter.WriteLine($"[P2P] 房主自动向成员 {user.DisplayName} 发起P2P连接");
                    SendPacket(new PeerConnectRequestPacket(user.UserId));
                }
            }
        }
        // 普通成员不主动发起P2P连接，只等待房主的连接请求
    }
    // 申请加入房间
    public void RequestJoinRoom(int roomId)
    {
        SendPacket(new RoomJoinRequestPacket(roomId));
    }
    // 房主审批同意用户加入
    public void ApproveJoinRoom(int roomId, string userId)
    {
        SendPacket(new RoomJoinApprovePacket(roomId, userId));
        _pendingJoinUserIds.Remove(userId);
    }
    // 房主拒绝用户加入
    public void RejectJoinRoom(int roomId, string userId)
    {
        SendPacket(new RoomJoinRejectPacket(roomId, userId));
        _pendingJoinUserIds.Remove(userId);
    }
    // 房主主动转让房主
    public void TransferRoomOwner(int roomId, string newOwnerUserId)
    {
        SendPacket(new RoomTransferOwnerPacket(roomId, newOwnerUserId));
    }
    // 房主主动解散房间
    public void DismissRoom(int roomId)
    {
        SendPacket(new RoomDismissPacket(roomId));
    }
    // 踢人（房主主动调用）
    public void KickUserFromRoom(int roomId, string userId)
    {
        SendPacket(new RoomKickPacket(roomId, userId));
    }
    // 查询用户信息
    public void QueryUserInfo(string userId)
    {
        SendPacket(new UserInfoQueryPacket(userId));
    }

    public ClientCommandManager GetClientCommandManager()
    {
        return ClientCommandManager;
    }

    /// <summary>
    /// 发送P2P心跳包
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    public async Task SendPeerKeepAliveAsync(string targetUserId)
    {
        if (PeerConnectionManager.HasConnection(targetUserId))
        {
            await PeerConnectionManager.SendKeepAliveAsync(targetUserId);
        }
        else
        {
            ConsoleWriter.WriteLine($"[P2P] 与用户 {targetUserId} 的P2P连接不存在");
        }
    }

    /// <summary>
    /// 发送P2P心跳包（同步版本）
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    public void SendPeerKeepAlive(string targetUserId)
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        SendPeerKeepAliveAsync(targetUserId).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    /// <summary>
    /// 获取所有P2P连接
    /// </summary>
    /// <returns>活跃的P2P连接列表</returns>
    public IEnumerable<string> GetPeerConnections()
    {
        return PeerConnectionManager.GetActiveConnections();
    }

    /// <summary>
    /// 检查是否有P2P连接
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <returns>是否有连接</returns>
    public bool HasPeerConnection(string targetUserId)
    {
        return PeerConnectionManager.HasConnection(targetUserId);
    }

    /// <summary>
    /// 设置P2P心跳间隔
    /// </summary>
    /// <param name="intervalSeconds">心跳间隔（秒）</param>
    public void SetPeerHeartbeatInterval(int intervalSeconds)
    {
        PeerConnectionManager.SetHeartbeatInterval(intervalSeconds);
    }

    /// <summary>
    /// 获取P2P心跳状态
    /// </summary>
    /// <returns>心跳状态信息</returns>
    public Dictionary<string, object> GetPeerKeepAliveStatus()
    {
        return PeerConnectionManager.GetKeepAliveStatus();
    }

    /// <summary>
    /// 关闭与指定用户的P2P连接
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    public void ClosePeerConnection(string targetUserId)
    {
        PeerConnectionManager.CloseConnection(targetUserId);
    }

    /// <summary>
    /// 关闭所有P2P连接
    /// </summary>
    public void CloseAllPeerConnections()
    {
        PeerConnectionManager.CloseAllConnections();
    }

    /// <summary>
    /// 获取与指定用户的P2P连接状态
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <returns>连接状态</returns>
    public ConnectionStatus GetPeerConnectionStatus(string targetUserId)
    {
        return PeerConnectionManager.GetConnectionStatus(targetUserId);
    }

    /// <summary>
    /// 发送P2P数据包
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="packet">数据包</param>
    public async Task SendPeerPacketAsync(string targetUserId, IPeerBoundPacket packet)
    {
        await PeerConnectionManager.SendPacketAsync(targetUserId, packet);
    }

    /// <summary>
    /// 获取综合代理统计信息
    /// </summary>
    /// <returns>综合代理统计信息</returns>
    public ProxyStatistics GetProxyStatistics()
    {
        return new ProxyStatistics
        {
            LocalProxyStats = _localProxyServer?.GetLocalProxyStatistics(),
            ProxyManagerStats = _proxyManager?.GetProxyStatistics()
        };
    }

    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WrapClient));
    }

    /// <summary>
    /// 初始化代理功能
    /// </summary>
    private async Task InitializeProxyAsync()
    {
        try
        {
            if (!_profile.EnableProxyForwarding)
            {
                ConsoleWriter.WriteLine("[代理] 代理转发功能已禁用");
                return;
            }

            // 等待一段时间让P2P连接建立
            await Task.Delay(2000);

            // 根据用户角色初始化不同的代理功能
            if (CurrentRoomInfo != null)
            {
                var isOwner = CurrentRoomInfo.Owner.UserId == _userInfo?.UserId;

                if (isOwner)
                {
                    // 房主：初始化代理管理器
                    _proxyManager = new ProxyManager(this);
                    ConsoleWriter.WriteLine("[代理] 房主代理管理器已初始化");
                }
                else
                {
                    // 检查是否有到房主的P2P连接
                    var ownerUserId = CurrentRoomInfo.Owner.UserId;
                    if (PeerConnectionManager.HasConnection(ownerUserId))
                    {
                        // 普通用户：初始化本地代理服务器
                        _localProxyServer = new LocalProxyServer(this, _profile.LocalProxyPort,
                            _profile.ProxyTargetAddress, _profile.ProxyTargetPort);

                        await _localProxyServer.StartAsync();
                        ConsoleWriter.WriteLine($"[代理] 普通用户本地代理服务器已启动，监听端口: {_profile.LocalProxyPort}");
                    }
                    else
                    {
                        ConsoleWriter.WriteLine($"[代理] 等待与房主 {ownerUserId} 的P2P连接建立...");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[代理] 初始化代理功能时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测NAT类型
    /// </summary>
    private async Task DetectNatTypeAsync()
    {
        try
        {
            ConsoleWriter.WriteLine("[NAT] 开始检测NAT类型...");

            ClassicStunResult result;

            // 尝试使用UPnP服务
            if (UPnPService != null)
            {
                IPEndPoint endPoint = new(new IPAddress(new byte[] { 0, 0, 0, 0 }), Random.Shared.Next(1000, 65535));
                UPnPService.AddPortMapping(endPoint.Port, SocketProtocol.UDP, endPoint.Port, "Wrap NAT test");
                result = StunHelper.GetClassicStunResultAsync(endPoint).GetAwaiter().GetResult();
                UPnPService.DeletePortMapping(endPoint.Port, SocketProtocol.UDP);
            }
            else
                result = await StunHelper.GetClassicStunResultAsync();

            CurrentNatType = result.NatType;
            PublicEndPoint = result.PublicEndPoint;

            ConsoleWriter.WriteLine($"[NAT] NAT类型检测完成: {CurrentNatType}");
            if (PublicEndPoint != null)
            {
                ConsoleWriter.WriteLine($"[NAT] 公网端点: {PublicEndPoint}");
            }

            NatTypeDetected?.Invoke(this, CurrentNatType);
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[NAT] NAT类型检测失败: {ex.Message}");
            CurrentNatType = NatType.Unknown;
        }
    }

    /// <summary>
    /// 建立P2P连接
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="peerEndPoint">对等端端点</param>
    private async Task EstablishP2PConnectionAsync(string targetUserId, IPEndPoint peerEndPoint)
    {
        ConsoleWriter.WriteLine($"[P2P] 开始建立与 {targetUserId} 的P2P连接，目标端点: {peerEndPoint}");

        // 创建本地TCP客户端
        var client = new TcpClient();
        client.Client.ExclusiveAddressUse = false;
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        // 绑定到本地端点
        client.Client.Bind(_clientChannel!.LocalAddress);

        bool connectionSucceeded = false;
        TcpClient? connectionToPeer = null;

        // 尝试连接任务
        var connectTask = Task.Run(() =>
        {
            for (int i = 0; i < 8; i++)
            {
                try
                {
                    ConsoleWriter.WriteLine($"[P2P] 尝试连接到 {peerEndPoint} (第{i + 1}次)");
                    client.Connect(peerEndPoint);
                    connectionSucceeded = true;
                    connectionToPeer = client;
                    ConsoleWriter.WriteLine($"[P2P] 连接成功");
                    break;
                }
                catch (Exception ex)
                {
                    ConsoleWriter.WriteLine($"[P2P] 连接失败 (第{i + 1}次): {ex.Message}");
                    if (i < 3) // 不是最后一次尝试
                    {
                        Thread.Sleep(1000); // 等待1秒后重试
                    }
                }
            }
        });

        // 监听任务
        TcpListener? listener = null;
        var listenTask = Task.Run(() =>
        {
            try
            {
                IPEndPoint endPoint = ((IPEndPoint)_clientChannel!.LocalAddress);
                listener = new TcpListener(endPoint.Address, endPoint.Port);
                listener.Server.ExclusiveAddressUse = false;
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Start();

                ConsoleWriter.WriteLine($"[P2P] 开始监听连接，本地端点: {((IPEndPoint)listener.LocalEndpoint)}");

                while (!connectionSucceeded)
                {
                    try
                    {
                        var peer = listener.AcceptTcpClient();
                        ConsoleWriter.WriteLine($"[P2P] 收到来自 {((IPEndPoint)peer.Client.RemoteEndPoint!)} 的连接");

                        if (connectionToPeer == null)
                        {
                            connectionToPeer = peer;
                            connectionSucceeded = true;
                        }
                        else
                        {
                            peer.Close();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!connectionSucceeded)
                        {
                            ConsoleWriter.WriteLine($"[P2P] 监听连接时出错: {ex.Message}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteLine($"[P2P] 启动监听器失败: {ex.Message}");
            }
        });

        // 等待连接完成
        await Task.WhenAny(connectTask, listenTask);

        // 清理监听器
        listener?.Stop();

        if (connectionToPeer == null)
        {
            client.Close();
            ConsoleWriter.WriteLine($"[P2P] 无法建立与 {targetUserId} 的P2P连接");

            // 通知服务器连接失败
            await SendPacketAsync(new PeerConnectFailedPacket(
                targetUserId,
                "无法建立P2P连接",
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            ));
            return;
        }

        // 创建P2P连接
        try
        {
            ConsoleWriter.WriteLine($"[P2P] 与 {targetUserId} 的P2P连接建立成功");

            // 使用P2P连接管理器管理连接
            PeerConnectionManager.AddConnection(targetUserId, connectionToPeer);

            // 通知服务器连接成功
            await SendPacketAsync(new PeerConnectSuccessPacket(targetUserId));
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[P2P] 创建P2P连接时出错: {ex.Message}");
            connectionToPeer.Close();

            // 通知服务器连接失败
            await SendPacketAsync(new PeerConnectFailedPacket(
                targetUserId,
                ex.Message,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            ));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            // 清理代理资源
            _proxyManager?.Dispose();
            _localProxyServer?.Dispose();

            // 释放P2P连接管理器
            PeerConnectionManager?.Dispose();

            UPnPService?.DeletePortMapping(RemoteIP.Port, IUPnPService.SocketProtocol.TCP);

            // 异步断开连接，但不等待完成
            _ = DisconnectAsync();
        }
        catch (Exception)
        {
            // 忽略断开连接时的错误
        }
    }
}

