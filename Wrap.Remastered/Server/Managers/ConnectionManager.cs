using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wrap.Remastered.Network.Pool;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Server.Managers;

/// <summary>
/// 连接管理器，提供高级连接管理功能
/// </summary>
public class ConnectionManager : IDisposable
{
    private readonly NetworkPool _networkPool;
    private readonly Timer _cleanupTimer;
    private readonly Timer _statisticsTimer;
    private volatile bool _disposed = false;

    /// <summary>
    /// 连接事件
    /// </summary>
    public event EventHandler<UserConnectionEventArgs>? UserConnected;
    public event EventHandler<UserConnectionEventArgs>? UserDisconnected;
    public event EventHandler<ConnectionStatisticsEventArgs>? StatisticsUpdated;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="maxConnections">最大连接数</param>
    /// <param name="cleanupInterval">清理间隔（毫秒）</param>
    /// <param name="statisticsInterval">统计更新间隔（毫秒）</param>
    public ConnectionManager(int maxConnections, int cleanupInterval = 30000, int statisticsInterval = 10000)
    {
        _networkPool = new NetworkPool(maxConnections);

        // 启动定期清理任务
        _cleanupTimer = new Timer(CleanupDisconnectedConnections, null, cleanupInterval, cleanupInterval);

        // 启动统计更新任务
        _statisticsTimer = new Timer(UpdateStatistics, null, statisticsInterval, statisticsInterval);
    }

    /// <summary>
    /// 最大连接数
    /// </summary>
    public int MaxConnections => _networkPool.MaxConnections;

    /// <summary>
    /// 当前连接数
    /// </summary>
    public int CurrentConnections => _networkPool.CurrentConnections;

    /// <summary>
    /// 可用连接数
    /// </summary>
    public int AvailableConnections => _networkPool.AvailableConnections;

    /// <summary>
    /// 连接池是否已满
    /// </summary>
    public bool IsFull => _networkPool.IsFull;

    /// <summary>
    /// 连接池是否为空
    /// </summary>
    public bool IsEmpty => _networkPool.IsEmpty;

    /// <summary>
    /// 添加用户连接
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    /// <param name="socket">网络套接字</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功添加连接</returns>
    public async Task<bool> AddUserConnectionAsync(UserInfo userInfo, Socket socket, CancellationToken cancellationToken = default)
    {
        CheckDisposed();

        if (userInfo == null)
            throw new ArgumentNullException(nameof(userInfo));
        if (socket == null)
            throw new ArgumentNullException(nameof(socket));

        var success = await _networkPool.TryAddUserConnectionAsync(userInfo, socket, cancellationToken);

        if (success)
        {
            var connection = _networkPool.GetUserConnection(userInfo.UserId);
            if (connection != null)
            {
                UserConnected?.Invoke(this, new UserConnectionEventArgs(connection));
            }
        }

        return success;
    }

    /// <summary>
    /// 移除用户连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveUserConnection(string userId)
    {
        CheckDisposed();

        var connection = _networkPool.GetUserConnection(userId);
        var success = _networkPool.RemoveUserConnection(userId);

        if (success && connection != null)
        {
            UserDisconnected?.Invoke(this, new UserConnectionEventArgs(connection));
        }

        return success;
    }

    /// <summary>
    /// 移除端点连接
    /// </summary>
    /// <param name="endpoint">端点</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveEndpointConnection(IPEndPoint endpoint)
    {
        CheckDisposed();

        var connection = _networkPool.GetEndpointConnection(endpoint);
        var success = _networkPool.RemoveEndpointConnection(endpoint);

        if (success && connection != null)
        {
            UserDisconnected?.Invoke(this, new UserConnectionEventArgs(connection));
        }

        return success;
    }

    /// <summary>
    /// 获取用户连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户连接</returns>
    public UserConnection? GetUserConnection(string userId)
    {
        CheckDisposed();
        return _networkPool.GetUserConnection(userId);
    }

    /// <summary>
    /// 获取端点连接
    /// </summary>
    /// <param name="endpoint">端点</param>
    /// <returns>用户连接</returns>
    public UserConnection? GetEndpointConnection(IPEndPoint endpoint)
    {
        CheckDisposed();
        return _networkPool.GetEndpointConnection(endpoint);
    }

    /// <summary>
    /// 获取所有连接
    /// </summary>
    /// <returns>所有连接</returns>
    public IEnumerable<UserConnection> GetAllConnections()
    {
        CheckDisposed();
        return _networkPool.GetAllConnections();
    }

    /// <summary>
    /// 获取所有用户ID
    /// </summary>
    /// <returns>所有用户ID</returns>
    public IEnumerable<string> GetAllUserIds()
    {
        CheckDisposed();
        return _networkPool.GetAllUserIds();
    }

    /// <summary>
    /// 检查用户是否已连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否已连接</returns>
    public bool IsUserConnected(string userId)
    {
        CheckDisposed();
        return _networkPool.IsUserConnected(userId);
    }

    /// <summary>
    /// 检查端点是否已连接
    /// </summary>
    /// <param name="endpoint">端点</param>
    /// <returns>是否已连接</returns>
    public bool IsEndpointConnected(IPEndPoint endpoint)
    {
        CheckDisposed();
        return _networkPool.IsEndpointConnected(endpoint);
    }

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    /// <returns>连接统计信息</returns>
    public NetworkPoolStatistics GetStatistics()
    {
        CheckDisposed();
        return _networkPool.GetStatistics();
    }

    /// <summary>
    /// 等待可用连接槽位
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功等待</returns>
    public async Task<bool> WaitForAvailableConnectionAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        return await _networkPool.WaitForAvailableConnectionAsync(timeout, cancellationToken);
    }

    /// <summary>
    /// 广播消息给所有连接的用户
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <returns>成功发送的连接数</returns>
    public async Task<int> BroadcastMessageAsync(byte[] message, string? excludeUserId = null)
    {
        CheckDisposed();

        var connections = _networkPool.GetAllConnections()
            .Where(conn => conn.IsConnected && (excludeUserId == null || conn.UserInfo.UserId != excludeUserId))
            .ToList();

        var tasks = connections.Select(async connection =>
        {
            try
            {
                await connection.Socket.SendAsync(message, SocketFlags.None);
                connection.UpdateActivity();
                return true;
            }
            catch (Exception)
            {
                // 发送失败，标记连接为断开
                connection.Close();
                return false;
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Count(x => x);
    }

    /// <summary>
    /// 发送消息给指定用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="message">消息内容</param>
    /// <returns>是否成功发送</returns>
    public async Task<bool> SendMessageToUserAsync(string userId, byte[] message)
    {
        CheckDisposed();

        var connection = _networkPool.GetUserConnection(userId);
        if (connection == null || !connection.IsConnected)
            return false;

        try
        {
            await connection.Socket.SendAsync(message, SocketFlags.None);
            connection.UpdateActivity();
            return true;
        }
        catch (Exception)
        {
            connection.Close();
            return false;
        }
    }

    /// <summary>
    /// 发送消息给指定端点
    /// </summary>
    /// <param name="endpoint">端点</param>
    /// <param name="message">消息内容</param>
    /// <returns>是否成功发送</returns>
    public async Task<bool> SendMessageToEndpointAsync(IPEndPoint endpoint, byte[] message)
    {
        CheckDisposed();

        var connection = _networkPool.GetEndpointConnection(endpoint);
        if (connection == null || !connection.IsConnected)
            return false;

        try
        {
            await connection.Socket.SendAsync(message, SocketFlags.None);
            connection.UpdateActivity();
            return true;
        }
        catch (Exception)
        {
            connection.Close();
            return false;
        }
    }

    /// <summary>
    /// 获取活跃连接数
    /// </summary>
    /// <returns>活跃连接数</returns>
    public int GetActiveConnectionCount()
    {
        CheckDisposed();
        return _networkPool.GetAllConnections().Count(conn => conn.IsConnected);
    }

    /// <summary>
    /// 获取断开连接数
    /// </summary>
    /// <returns>断开连接数</returns>
    public int GetDisconnectedCount()
    {
        CheckDisposed();
        return _networkPool.GetAllConnections().Count(conn => !conn.IsConnected);
    }

    /// <summary>
    /// 强制清理所有断开连接
    /// </summary>
    public void ForceCleanup()
    {
        CheckDisposed();
        _networkPool.CleanupDisconnectedConnections();
    }

    private void CleanupDisconnectedConnections(object? state)
    {
        try
        {
            _networkPool.CleanupDisconnectedConnections();
        }
        catch (Exception)
        {
            // 忽略清理过程中的异常
        }
    }

    private void UpdateStatistics(object? state)
    {
        try
        {
            var statistics = _networkPool.GetStatistics();
            StatisticsUpdated?.Invoke(this, new ConnectionStatisticsEventArgs(statistics));
        }
        catch (Exception)
        {
            // 忽略统计更新过程中的异常
        }
    }

    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConnectionManager));
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        _cleanupTimer?.Dispose();
        _statisticsTimer?.Dispose();
        _networkPool?.Dispose();
    }
}

/// <summary>
/// 用户连接事件参数
/// </summary>
public class UserConnectionEventArgs : EventArgs
{
    /// <summary>
    /// 用户连接
    /// </summary>
    public UserConnection Connection { get; }

    public UserConnectionEventArgs(UserConnection connection)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
}
