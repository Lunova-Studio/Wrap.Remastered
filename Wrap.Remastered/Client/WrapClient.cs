using ConsoleInteractive;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System.Net;
using System.Net.Sockets;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Client;

public class WrapClient : IWrapClient, IDisposable
{
    private UserInfo? _userInfo;
    private IEventLoopGroup? _eventLoopGroup;
    private IChannel? _clientChannel;
    private volatile bool _isConnected = false;
    private volatile bool _disposed = false;
    private volatile bool _isLoggedIn = false;
    private UserProfile _profile = UserProfile.Load();
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

    public bool Disposed => _disposed;
    public bool IsConnected => _isConnected && _clientChannel?.Active == true;
    public bool IsLoggedIn => _isLoggedIn;

    public string? UserId => _userInfo?.UserId;
    public string? Name => _userInfo?.Name ?? _profile.Name;
    public string? DisplayName => _userInfo?.DisplayName ?? _profile.DisplayName;
    public UserProfile Profile => _profile;
    public ClientCommandManager ClientCommandManager { get; private set; }

    public event EventHandler? Connected;
    public event EventHandler<string>? Disconnected;
    public event EventHandler<UnsolvedPacket>? DataReceived;
    public event EventHandler<IClientBoundPacket>? PacketReceived;
    public event EventHandler<UserInfo>? LoggedIn;

    /// <summary>
    /// 无参数构造函数
    /// </summary>
    public WrapClient()
    {
        ClientCommandManager = new ClientCommandManager(this);
    }

    /// <summary>
    /// 带用户信息的构造函数
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    public WrapClient(UserInfo userInfo)
    {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        ClientCommandManager = new ClientCommandManager(this);
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
            // 解析服务器地址，确保使用IPv4
            IPAddress? ipAddress = null;
            if (serverAddress.ToLower() == "localhost")
            {
                ipAddress = IPAddress.Loopback;
            }
            else if (!IPAddress.TryParse(serverAddress, out ipAddress))
            {
                var hostEntry = await Dns.GetHostEntryAsync(serverAddress);
                ipAddress = hostEntry.AddressList.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);

                if (ipAddress == null)
                {
                    throw new ArgumentException($"无法解析服务器地址: {serverAddress}");
                }
            }

            // 确保使用IPv4地址
            if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                throw new ArgumentException($"服务器地址必须是IPv4地址: {serverAddress}");
            }

            _eventLoopGroup = new MultithreadEventLoopGroup(1);

            var bootstrap = new Bootstrap();
            bootstrap.Group(_eventLoopGroup)
                   .Channel<TcpSocketChannel>()
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
            _clientChannel = await bootstrap.ConnectAsync(endpoint);
            _isConnected = true;

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

    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WrapClient));
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            // 异步断开连接，但不等待完成
            _ = DisconnectAsync();
        }
        catch (Exception)
        {
            // 忽略断开连接时的错误
        }
    }
}

