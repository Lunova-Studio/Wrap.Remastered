using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Server;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Handlers.Logging;
using DotNetty.Codecs;
using System.Net;
using Wrap.Remastered.Network.Protocol.ServerBound;
using System.Net.Sockets;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Client;

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

    public bool Disposed => _disposed;
    public bool IsConnected => _isConnected && _clientChannel?.Active == true;
    public bool IsLoggedIn => _isLoggedIn;

    public string? UserId => _userInfo?.UserId;
    public string? Name => _userInfo?.Name ?? _profile.Name;
    public string? DisplayName => _userInfo?.DisplayName ?? _profile.DisplayName;
    public UserProfile Profile => _profile;

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
    }

    /// <summary>
    /// 带用户信息的构造函数
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    public WrapClient(UserInfo userInfo)
    {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
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
        ConnectAsync(serverAddress, port).GetAwaiter().GetResult();
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
        DisconnectAsync().GetAwaiter().GetResult();
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
        catch (Exception ex)
        {
            throw;
        }
    }

    public void SendPacket(IServerBoundPacket packet)
    {
        SendPacketAsync(packet).GetAwaiter().GetResult();
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

