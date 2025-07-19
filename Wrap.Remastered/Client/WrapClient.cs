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

namespace Wrap.Remastered.Client;

public class WrapClient : IWrapClient, IDisposable
{
    private readonly UserInfo _userInfo;
    private IEventLoopGroup? _eventLoopGroup;
    private IChannel? _clientChannel;
    private volatile bool _isConnected = false;
    private volatile bool _disposed = false;

    public bool Disposed => _disposed;
    public bool IsConnected => _isConnected && _clientChannel?.Active == true;

    public string UserId => _userInfo.UserId;
    public string Name => _userInfo.Name;
    public string DisplayName => _userInfo.DisplayName;

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<UnsolvedPacket>? DataReceived;
    public event EventHandler<IClientBoundPacket>? PacketReceived;

    public WrapClient(UserInfo userInfo)
    {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
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
            Console.WriteLine($"正在连接到服务器: {serverAddress}:{port}");

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

            Console.WriteLine($"解析的IP地址: {ipAddress}");

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

            await SendLoginPacketAsync();

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

            Disconnected?.Invoke(this, EventArgs.Empty);
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
                var buffer = Unpooled.WrappedBuffer(data);
                await _clientChannel.WriteAsync(Unpooled.CopyInt((int)packet.GetPacketType()));
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
        SendPacketAsync(packet).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 发送登录数据包
    /// </summary>
    private async Task SendLoginPacketAsync()
    {
        var loginPacket = new LoginPacket(_userInfo);
        await SendPacketAsync(loginPacket);
    }

    /// <summary>
    /// 处理接收到的数据
    /// </summary>
    /// <param name="data">接收到的数据</param>
    internal void OnDataReceived(UnsolvedPacket unsolved)
    {
        try
        {
            DataReceived?.Invoke(this, unsolved);

            ISerializer<IPacket> serializer = IClientBoundPacket.Serializers[(ClientBoundPacketType)unsolved.PacketType];
            IClientBoundPacket packet = (IClientBoundPacket)serializer.Deserialize(unsolved.Data);
            
            PacketReceived?.Invoke(this, packet);
        }
        catch (Exception) { }
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

        Disconnect();
        GC.SuppressFinalize(this);
    }
}

