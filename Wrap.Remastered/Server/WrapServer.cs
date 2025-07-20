using ConsoleInteractive;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System.Net;
using System.Net.Sockets;
using Wrap.Remastered.Commands;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Server.Configuration;
using Wrap.Remastered.Server.Events;
using Wrap.Remastered.Server.Handlers;
using Wrap.Remastered.Server.Managers;
using Wrap.Remastered.Server.Services;

namespace Wrap.Remastered.Server;

public class WrapServer : IWrapServer, IDisposable
{
    private readonly ServerConfiguration _configuration;
    public ServerConfiguration Configuration => _configuration;

    private IEventLoopGroup? _bossGroup;
    private IEventLoopGroup? _workerGroup;
    private IChannel? _serverChannel;

    private DotNettyConnectionManager? _connectionManager;
    private CommandManager _commandManager;
    private LoggingService _loggingService;
    private RoomManager _roomManager = new RoomManager();
    private PeerManager _peerManager = new PeerManager();


    private CancellationTokenSource? _statisticsCancellationTokenSource;
    private CancellationTokenSource? _keepAliveCancellationTokenSource;

    private volatile bool _disposed = false;
    private volatile bool _isRunning = false;

    public bool Disposed => _disposed;
    public bool IsRunning => _isRunning;
    public int Port => _configuration.Port;
    public int MaxConnections => _configuration.MaxConnections;
    public int CurrentConnections => _connectionManager?.CurrentConnections ?? 0;
    public int CurrentUserConnections => _connectionManager?.CurrentUserConnections ?? 0;

    public event EventHandler<ServerStartedEventArgs>? ServerStarted;
    public event EventHandler<ServerStoppedEventArgs>? ServerStopped;
    public event EventHandler<ClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;
    public event EventHandler<DataReceivedEventArgs>? DataReceived;

    public WrapServer(ServerConfiguration? configuration = null)
    {
        _configuration = configuration ?? ServerConfiguration.CreateDefault();
        _configuration.Validate();

        _commandManager = new CommandManager();
        _loggingService = new LoggingService(this);

        _loggingService.InitializeServerCommands();
    }

    public WrapServer(int port = 10270, int bossThreads = 1, int workerThreads = 4, int maxConnections = 1000)
    {
        _configuration = new ServerConfiguration
        {
            Port = port,
            BossThreads = bossThreads,
            WorkerThreads = workerThreads,
            MaxConnections = maxConnections
        };
        _configuration.Validate();

        _commandManager = new CommandManager();
        _loggingService = new LoggingService(this);

        _loggingService.InitializeServerCommands();
    }

    public async Task StartAsync()
    {
        CheckDisposed();

        if (_isRunning)
        {
            throw new InvalidOperationException("服务器已经在运行中");
        }

        try
        {
            Console.WriteLine($"正在启动服务器，端口: {_configuration.Port}");

            // 创建事件循环组
            _bossGroup = new MultithreadEventLoopGroup(_configuration.BossThreads);
            _workerGroup = new MultithreadEventLoopGroup(_configuration.WorkerThreads);

            // 创建连接管理器
            _connectionManager = new DotNettyConnectionManager(this);

            // 注册事件处理器
            _connectionManager.ClientConnected += OnClientConnected;
            _connectionManager.ClientDisconnected += OnClientDisconnected;

            // 创建服务器引导程序
            var bootstrap = new ServerBootstrap();
            bootstrap.Group(_bossGroup, _workerGroup)
                   .ChannelFactory(() => new TcpServerSocketChannel(AddressFamily.InterNetwork))
                   .Option(ChannelOption.SoBacklog, 128)
                   .Option(ChannelOption.SoReuseaddr, true)
                   .Option(ChannelOption.SoReuseport, true)
                   .Option(ChannelOption.SoKeepalive, true)
                   .Option(ChannelOption.TcpNodelay, true)
                   .ChildOption(ChannelOption.SoKeepalive, true)
                   .ChildOption(ChannelOption.TcpNodelay, true)
                   .Handler(new LoggingHandler("Boss"))
                   .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                   {
                       var pipeline = channel.Pipeline;

                       pipeline.AddLast(new LoggingHandler("Worker"));
                       pipeline.AddLast(new LengthFieldBasedFrameDecoder(65536, 0, 4, 0, 4));
                       pipeline.AddLast(new LengthFieldPrepender(4));
                       pipeline.AddLast(new ServerHandler(this, _workerGroup));
                   }));

            // 只绑定IPv4地址，拒绝IPv6连接
            var endpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), _configuration.Port);
            Console.WriteLine($"服务器绑定到IPv4地址: {endpoint}");
            _serverChannel = await bootstrap.BindAsync(endpoint);
            _isRunning = true;

            Console.WriteLine($"服务器启动成功，监听端口: {_configuration.Port}");
            Console.WriteLine($"最大连接数: {_configuration.MaxConnections}");
            Console.WriteLine($"Boss线程数: {_configuration.BossThreads}");
            Console.WriteLine($"Worker线程数: {_configuration.WorkerThreads}");
            Console.WriteLine($"IPv4专用模式: {(_configuration.IPv4Only ? "启用" : "禁用")}");
            Console.WriteLine($"实际绑定地址: {_serverChannel.LocalAddress}");

            // 验证绑定地址是否为IPv4
            if (_serverChannel.LocalAddress is IPEndPoint localEndPoint)
            {
                if (localEndPoint.AddressFamily != AddressFamily.InterNetwork)
                {
                    Console.WriteLine($"警告: 服务器绑定到了非IPv4地址: {localEndPoint.AddressFamily}");
                }
                else
                {
                    Console.WriteLine($"✓ 服务器成功绑定到IPv4地址: {localEndPoint}");
                }
            }

            ServerStarted?.Invoke(this, new ServerStartedEventArgs(_configuration.Port));

            // 启动统计信息输出任务
            _statisticsCancellationTokenSource = new CancellationTokenSource();
            _ = OutputStatisticsAsync();

            // 启动KeepAlive任务
            _keepAliveCancellationTokenSource = new CancellationTokenSource();
            _ = OutputKeepAliveAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动服务器时发生错误: {ex.Message}");
            await StopAsync();
            throw;
        }
    }

    /// <summary>
    /// 停止服务器
    /// </summary>
    public async Task StopAsync()
    {
        await BroadcastServerShutdownAsync();
        CheckDisposed();

        if (!_isRunning)
        {
            return;
        }

        try
        {
            Console.WriteLine("正在停止服务器...");

            _isRunning = false;

            // 停止KeepAlive任务
            if (_keepAliveCancellationTokenSource != null)
            {
                await _keepAliveCancellationTokenSource.CancelAsync();
                _keepAliveCancellationTokenSource.Dispose();
                _keepAliveCancellationTokenSource = null;
            }

            // 停止统计信息输出任务
            if (_statisticsCancellationTokenSource != null)
            {
                await _statisticsCancellationTokenSource.CancelAsync();
                _statisticsCancellationTokenSource.Dispose();
                _statisticsCancellationTokenSource = null;
            }

            // 释放连接管理器
            _connectionManager?.Dispose();
            _connectionManager = null;

            // 释放统计任务的取消令牌
            _statisticsCancellationTokenSource?.Dispose();
            _statisticsCancellationTokenSource = null;

            // 关闭服务器通道
            if (_serverChannel != null)
            {
                await _serverChannel.CloseAsync();
                _serverChannel = null;
            }

            // 关闭事件循环组（带超时）
            if (_bossGroup != null)
            {
                try
                {
                    await _bossGroup.ShutdownGracefullyAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"关闭Boss事件循环组时发生错误: {ex.Message}");
                }
                finally
                {
                    _bossGroup = null;
                }
            }

            if (_workerGroup != null)
            {
                try
                {
                    await _workerGroup.ShutdownGracefullyAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"关闭Worker事件循环组时发生错误: {ex.Message}");
                }
                finally
                {
                    _workerGroup = null;
                }
            }

            Console.WriteLine("服务器已停止");
            ServerStopped?.Invoke(this, new ServerStoppedEventArgs());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"停止服务器时发生错误: {ex.Message}");
            ServerStopped?.Invoke(this, new ServerStoppedEventArgs(ex.Message));
            throw;
        }
    }

    /// <summary>
    /// 广播数据给所有客户端
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>发送成功的客户端数量</returns>
    public async Task<int> BroadcastAsync(byte[] data)
    {
        CheckDisposed();

        if (_connectionManager == null)
            return 0;

        return await _connectionManager.BroadcastToAllAsync(data);
    }

    /// <summary>
    /// 发送数据给指定客户端
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="data">数据</param>
    /// <returns>是否发送成功</returns>
    public async Task<bool> SendToClientAsync(string clientId, byte[] data)
    {
        CheckDisposed();

        if (_connectionManager == null)
            return false;

        return await _connectionManager.SendDataToUserAsync(clientId, data);
    }

    /// <summary>
    /// 断开指定客户端连接
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <returns>是否断开成功</returns>
    public async Task<bool> DisconnectClientAsync(string clientId, string? reason = null)
    {
        CheckDisposed();
        if (_connectionManager == null)
            return false;
        if (_connectionManager is DotNettyConnectionManager dotNettyConnMgr)
        {
            return await dotNettyConnMgr.DisconnectUserAsync(clientId, reason);
        }
        var success = await _connectionManager.DisconnectUserAsync(clientId, reason);
        return await Task.FromResult(success);
    }

    /// <summary>
    /// 客户端连接事件处理
    /// </summary>
    private void OnClientConnected(object? sender, ChannelConnectionEventArgs e)
    {
        var clientId = e.Connection.UserId ?? e.Connection.RemoteAddress;
        ClientConnected?.Invoke(this, new ClientConnectedEventArgs(clientId, e.Connection.RemoteAddress));
    }

    /// <summary>
    /// 客户端断开事件处理
    /// </summary>
    private void OnClientDisconnected(object? sender, ChannelConnectionEventArgs e)
    {
        var clientId = e.Connection.UserId ?? e.Connection.RemoteAddress;
        // 用户断开时自动退出房间
        if (!string.IsNullOrEmpty(e.Connection.UserId))
        {
            var userId = e.Connection.UserId;

            // 从PeerManager移除用户
            _peerManager.RemoveUserConnection(userId);

            var roomId = _roomManager.GetUserRoomId(userId);
            if (roomId.HasValue)
            {
                _roomManager.RemoveUserFromRoom(roomId.Value, userId);
            }
        }
        ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(clientId));
    }

    /// <summary>
    /// 输出统计信息
    /// </summary>
    private async Task OutputStatisticsAsync()
    {
        while (!_statisticsCancellationTokenSource!.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(30000, _statisticsCancellationTokenSource.Token); // 30秒输出一次

                if (_connectionManager != null)
                {
                    var stats = _connectionManager.GetStatistics();
                    ConsoleWriter.WriteLine($"统计信息 - 总连接: {stats.TotalConnections}, 用户连接: {stats.UserConnections}, 活跃: {stats.ActiveConnections}, 非活跃: {stats.InactiveConnections}");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteLine($"输出统计信息时发生错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 输出KeepAlive包
    /// </summary>
    private async Task OutputKeepAliveAsync()
    {
        var random = new Random();
        while (!_keepAliveCancellationTokenSource!.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(10000, _keepAliveCancellationTokenSource.Token); // 10秒发送一次

                if (_connectionManager != null)
                {
                    var keepAliveValue = random.Next();
                    var keepAlivePacket = new KeepAlivePacket(keepAliveValue);

                    // 为每个连接设置期望的响应值
                    var connections = _connectionManager.GetAllConnections().ToList();
                    foreach (var connection in connections)
                    {
                        connection.SetExpectedKeepAliveValue(keepAliveValue);
                    }

                    await _connectionManager.BroadcastToUsersAsync(keepAlivePacket);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteLine($"发送KeepAlive包时发生错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 关闭服务器时，广播断开包
    /// </summary>
    private async Task BroadcastServerShutdownAsync()
    {
        var disconnectPacket = new Wrap.Remastered.Network.Protocol.ClientBound.DisconnectPacket
        {
            Reason = "服务器关闭",
            DisconnectTime = DateTime.UtcNow
        };

        if (_connectionManager != null)
            await _connectionManager.BroadcastToAllAsync(disconnectPacket);
    }

    /// <summary>
    /// 检查是否已释放
    /// </summary>
    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WrapServer));
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            // 异步停止服务器，但不等待完成
            _ = StopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"释放服务器时发生错误: {ex.Message}");
        }
    }

    public CommandManager GetCommandManager()
    {
        return _commandManager;
    }

    public IConnectionManager GetConnectionManager()
    {
        return _connectionManager!;
    }

    public LoggingService GetLoggingService()
    {
        return _loggingService;
    }

    public RoomManager GetRoomManager()
    {
        return _roomManager;
    }

    public PeerManager GetPeerManager()
    {
        return _peerManager;
    }
}

