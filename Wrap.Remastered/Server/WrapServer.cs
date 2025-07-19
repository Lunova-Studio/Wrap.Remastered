using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Server.Handlers;
using Wrap.Remastered.Server.Managers;

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
    
    private volatile bool _disposed = false;
    private volatile bool _isRunning = false;

    public bool Disposed => _disposed;
    public bool IsRunning => _isRunning;
    public int Port => _port;
    public int MaxConnections => _maxConnections;
    public int CurrentConnections => _connectionManager?.CurrentConnections ?? 0;
    public int CurrentUserConnections => _connectionManager?.CurrentUserConnections ?? 0;

    public event EventHandler? ServerStarted;
    public event EventHandler? ServerStopped;
    public event EventHandler<ChannelConnectionEventArgs>? ClientConnected;
    public event EventHandler<ChannelConnectionEventArgs>? ClientDisconnected;

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
            _connectionManager = new DotNettyConnectionManager();
            
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
                       pipeline.AddLast(new ServerHandler(_connectionManager, _workerGroup));
                   }));

            // 绑定端口并启动服务器
            var endpoint = new IPEndPoint(IPAddress.Any, _port);
            _serverChannel = await bootstrap.BindAsync(endpoint);
            _isRunning = true;

            Console.WriteLine($"服务器启动成功，监听端口: {_port}");
            Console.WriteLine($"最大连接数: {_maxConnections}");
            Console.WriteLine($"Boss线程数: {_bossThreads}");
            Console.WriteLine($"Worker线程数: {_workerThreads}");

            ServerStarted?.Invoke(this, EventArgs.Empty);

            // 启动统计信息输出任务
            _ = Task.Run(OutputStatisticsAsync);
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
        CheckDisposed();

        if (!_isRunning)
        {
            return;
        }

        try
        {
            Console.WriteLine("正在停止服务器...");

            _isRunning = false;

            // 关闭服务器通道
            if (_serverChannel != null)
            {
                await _serverChannel.CloseAsync();
                _serverChannel = null;
            }

            // 关闭事件循环组
            if (_bossGroup != null)
            {
                await _bossGroup.ShutdownGracefullyAsync();
                _bossGroup = null;
            }

            if (_workerGroup != null)
            {
                await _workerGroup.ShutdownGracefullyAsync();
                _workerGroup = null;
            }

            // 释放连接管理器
            _connectionManager?.Dispose();
            _connectionManager = null;

            Console.WriteLine("服务器已停止");
            ServerStopped?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"停止服务器时发生错误: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 启动服务器（同步版本）
    /// </summary>
    public void Start()
    {
        StartAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 关闭服务器（同步版本）
    /// </summary>
    public void Shutdown()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 发送数据给用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="data">数据</param>
    /// <returns>是否成功发送</returns>
    public async Task<bool> SendDataToUserAsync(string userId, byte[] data)
    {
        CheckDisposed();

        if (!_isRunning || _connectionManager == null)
            return false;

        return await _connectionManager.SendDataToUserAsync(userId, data);
    }

    /// <summary>
    /// 广播数据给所有用户
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <returns>成功发送的连接数</returns>
    public async Task BroadcastToUsersAsync(byte[] data, string? excludeUserId = null)
    {
        CheckDisposed();

        if (!_isRunning || _connectionManager == null)
            return;

        await _connectionManager.BroadcastToUsersAsync(data, excludeUserId);
    }

    /// <summary>
    /// 广播数据给所有连接
    /// </summary>
    /// <param name="data">数据</param>
    public async Task BroadcastToAllAsync(byte[] data)
    {
        CheckDisposed();

        if (!_isRunning || _connectionManager == null)
            return;

        await _connectionManager.BroadcastToAllAsync(data);
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="userInfo">用户信息</param>
    public void SetUserInfo(IChannel channel, UserInfo userInfo)
    {
        CheckDisposed();

        if (!_isRunning || _connectionManager == null)
            return;

        _connectionManager.SetUserInfo(channel, userInfo);
    }

    /// <summary>
    /// 获取用户连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>通道连接</returns>
    public ChannelConnection? GetUserConnection(string userId)
    {
        CheckDisposed();

        if (!_isRunning || _connectionManager == null)
            return null;

        return _connectionManager.GetUserConnection(userId);
    }

    /// <summary>
    /// 检查用户是否已连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否已连接</returns>
    public bool IsUserConnected(string userId)
    {
        CheckDisposed();

        if (!_isRunning || _connectionManager == null)
            return false;

        return _connectionManager.IsUserConnected(userId);
    }

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    /// <returns>连接统计信息</returns>
    public ConnectionStatistics? GetStatistics()
    {
        CheckDisposed();

        if (!_isRunning || _connectionManager == null)
            return null;

        return _connectionManager.GetStatistics();
    }

    /// <summary>
    /// 客户端连接事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnClientConnected(object? sender, ChannelConnectionEventArgs e)
    {
        Console.WriteLine($"客户端连接: {e.Connection.RemoteAddress}");
        ClientConnected?.Invoke(this, e);
    }

    /// <summary>
    /// 客户端断开事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnClientDisconnected(object? sender, ChannelConnectionEventArgs e)
    {
        Console.WriteLine($"客户端断开: {e.Connection.RemoteAddress}");
        ClientDisconnected?.Invoke(this, e);
    }

    /// <summary>
    /// 输出统计信息
    /// </summary>
    private async Task OutputStatisticsAsync()
    {
        while (_isRunning)
        {
            try
            {
                await Task.Delay(30000); // 每30秒输出一次统计信息

                if (_isRunning && _connectionManager != null)
                {
                    var stats = _connectionManager.GetStatistics();
                    Console.WriteLine($"服务器统计: 总连接 {stats.TotalConnections}, 用户连接 {stats.UserConnections}, 活跃连接 {stats.ActiveConnections}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"输出统计信息时发生错误: {ex.Message}");
            }
        }
    }

    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WrapServer));
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        // 停止服务器
        if (_isRunning)
        {
            StopAsync().GetAwaiter().GetResult();
        }

        GC.SuppressFinalize(this);
    }
}
