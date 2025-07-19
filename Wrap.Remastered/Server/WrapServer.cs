using ConsoleInteractive;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System.Net;
using Wrap.Remastered.Commands;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Server.Events;
using Wrap.Remastered.Server.Handlers;
using Wrap.Remastered.Server.Managers;
using Wrap.Remastered.Server.Services;

namespace Wrap.Remastered.Server;

public class WrapServer : IWrapServer, IDisposable
{
    private readonly int _port;
    private readonly int _bossThreads;
    private readonly int _workerThreads;
    private readonly int _maxConnections;

    private IEventLoopGroup? _bossGroup;
    private IEventLoopGroup? _workerGroup;
    private IChannel? _serverChannel;

    private DotNettyConnectionManager? _connectionManager;
    private CommandManager _commandManager;
    private LoggingService _loggingService;


    private CancellationTokenSource? _statisticsCancellationTokenSource;

    private volatile bool _disposed = false;
    private volatile bool _isRunning = false;

    public bool Disposed => _disposed;
    public bool IsRunning => _isRunning;
    public int Port => _port;
    public int MaxConnections => _maxConnections;
    public int CurrentConnections => _connectionManager?.CurrentConnections ?? 0;
    public int CurrentUserConnections => _connectionManager?.CurrentUserConnections ?? 0;

    public event EventHandler<ServerStartedEventArgs>? ServerStarted;
    public event EventHandler<ServerStoppedEventArgs>? ServerStopped;
    public event EventHandler<ClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;
    public event EventHandler<DataReceivedEventArgs>? DataReceived;

    public WrapServer(int port = 10270, int bossThreads = 1, int workerThreads = 4, int maxConnections = 1000)
    {
        if (port <= 0 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "端口号必须在1-65535之间");
        if (bossThreads <= 0)
            throw new ArgumentOutOfRangeException(nameof(bossThreads), "Boss线程数必须大于0");
        if (workerThreads <= 0)
            throw new ArgumentOutOfRangeException(nameof(workerThreads), "Worker线程数必须大于0");
        if (maxConnections <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConnections), "最大连接数必须大于0");

        _port = port;
        _bossThreads = bossThreads;
        _workerThreads = workerThreads;
        _maxConnections = maxConnections;

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
            Console.WriteLine($"正在启动服务器，端口: {_port}");

            // 创建事件循环组
            _bossGroup = new MultithreadEventLoopGroup(_bossThreads);
            _workerGroup = new MultithreadEventLoopGroup(_workerThreads);

            // 创建连接管理器
            _connectionManager = new DotNettyConnectionManager(this);

            // 注册事件处理器
            _connectionManager.ClientConnected += OnClientConnected;
            _connectionManager.ClientDisconnected += OnClientDisconnected;

            // 创建服务器引导程序
            var bootstrap = new ServerBootstrap();
            bootstrap.Group(_bossGroup, _workerGroup)
                   .Channel<TcpServerSocketChannel>()
                   .Option(ChannelOption.SoBacklog, 128)
                   .Option(ChannelOption.SoReuseaddr, true)
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

            var endpoint = new IPEndPoint(IPAddress.Any, _port);
            _serverChannel = await bootstrap.BindAsync(endpoint);
            _isRunning = true;

            Console.WriteLine($"服务器启动成功，监听端口: {_port}");
            Console.WriteLine($"最大连接数: {_maxConnections}");
            Console.WriteLine($"Boss线程数: {_bossThreads}");
            Console.WriteLine($"Worker线程数: {_workerThreads}");

            ServerStarted?.Invoke(this, new ServerStartedEventArgs(_port));

            // 启动统计信息输出任务
            _statisticsCancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(OutputStatisticsAsync, _statisticsCancellationTokenSource.Token);
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

            // 取消统计任务
            await _statisticsCancellationTokenSource?.CancelAsync();

            // 等待一小段时间让统计任务正常退出
            await Task.Delay(100);

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

        var success = _connectionManager.DisconnectUser(clientId, reason);
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
}

