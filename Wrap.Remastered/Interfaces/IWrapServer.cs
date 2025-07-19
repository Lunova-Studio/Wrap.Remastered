using Wrap.Remastered.Commands;
using Wrap.Remastered.Server.Events;
using Wrap.Remastered.Server.Handlers;
using Wrap.Remastered.Server.Services;
using Wrap.Remastered.Server.Managers;

namespace Wrap.Remastered.Interfaces;

/// <summary>
/// Wrap 服务器接口
/// </summary>
public interface IWrapServer : IDisposable
{
    /// <summary>
    /// 服务器启动事件
    /// </summary>
    event EventHandler<ServerStartedEventArgs>? ServerStarted;

    /// <summary>
    /// 服务器停止事件
    /// </summary>
    event EventHandler<ServerStoppedEventArgs>? ServerStopped;

    /// <summary>
    /// 客户端连接事件
    /// </summary>
    event EventHandler<ClientConnectedEventArgs>? ClientConnected;

    /// <summary>
    /// 客户端断开事件
    /// </summary>
    event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;

    /// <summary>
    /// 数据接收事件
    /// </summary>
    event EventHandler<DataReceivedEventArgs>? DataReceived;

    /// <summary>
    /// 服务器是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 服务器端口
    /// </summary>
    int Port { get; }

    /// <summary>
    /// 最大连接数
    /// </summary>
    int MaxConnections { get; }

    /// <summary>
    /// 当前连接数
    /// </summary>
    int CurrentConnections { get; }

    /// <summary>
    /// 启动服务器
    /// </summary>
    /// <returns>启动任务</returns>
    Task StartAsync();

    /// <summary>
    /// 停止服务器
    /// </summary>
    /// <returns>停止任务</returns>
    Task StopAsync();

    /// <summary>
    /// 广播数据给所有客户端
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>发送成功的客户端数量</returns>
    Task<int> BroadcastAsync(byte[] data);

    /// <summary>
    /// 发送数据给指定客户端
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="data">数据</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendToClientAsync(string clientId, byte[] data);

    /// <summary>
    /// 断开指定客户端连接
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <returns>是否断开成功</returns>
    Task<bool> DisconnectClientAsync(string clientId, string? reason = null);

    CommandManager GetCommandManager();

    IConnectionManager GetConnectionManager();

    LoggingService GetLoggingService();

    RoomManager GetRoomManager();
}
