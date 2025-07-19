using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Server;

namespace Wrap.Remastered.Network.Pool;

/// <summary>
/// 线程安全的网络连接池，支持用户连接管理
/// </summary>
public class NetworkPool : IDisposable
{
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly ConcurrentDictionary<string, UserConnection> _userConnections;
    private readonly ConcurrentDictionary<IPEndPoint, UserConnection> _endpointConnections;
    private readonly object _lockObject = new object();
    private volatile bool _disposed = false;

    /// <summary>
    /// 最大连接数
    /// </summary>
    public int MaxConnections { get; private set; }

    /// <summary>
    /// 当前连接数
    /// </summary>
    public int CurrentConnections => _userConnections.Count;

    /// <summary>
    /// 可用连接数
    /// </summary>
    public int AvailableConnections => MaxConnections - CurrentConnections;

    /// <summary>
    /// 连接池是否已满
    /// </summary>
    public bool IsFull => CurrentConnections >= MaxConnections;

    /// <summary>
    /// 连接池是否为空
    /// </summary>
    public bool IsEmpty => CurrentConnections == 0;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="maxConnections">最大连接数</param>
    public NetworkPool(int maxConnections)
    {
        if (maxConnections <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConnections), "最大连接数必须大于零。");

        MaxConnections = maxConnections;
        _connectionSemaphore = new SemaphoreSlim(maxConnections, maxConnections);
        _userConnections = new ConcurrentDictionary<string, UserConnection>();
        _endpointConnections = new ConcurrentDictionary<IPEndPoint, UserConnection>();
    }

    /// <summary>
    /// 尝试添加用户连接
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    /// <param name="socket">网络套接字</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功添加连接</returns>
    public async Task<bool> TryAddUserConnectionAsync(UserInfo userInfo, Socket socket, CancellationToken cancellationToken = default)
    {
        CheckDisposed();

        if (userInfo == null)
            throw new ArgumentNullException(nameof(userInfo));
        if (socket == null)
            throw new ArgumentNullException(nameof(socket));

        // 等待获取连接槽位
        if (!await _connectionSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return false; // 超时，无法获取连接槽位
        }

        try
        {
            var userConnection = new UserConnection(userInfo, socket);
            
            // 检查用户是否已存在连接
            if (_userConnections.TryGetValue(userInfo.UserId, out var existingConnection))
            {
                // 移除旧连接
                RemoveUserConnectionInternal(existingConnection);
            }

            // 添加新连接
            if (_userConnections.TryAdd(userInfo.UserId, userConnection) &&
                _endpointConnections.TryAdd(socket.RemoteEndPoint as IPEndPoint, userConnection))
            {
                // 设置连接断开事件处理
                userConnection.ConnectionClosed += OnConnectionClosed;
                return true;
            }

            return false;
        }
        catch
        {
            _connectionSemaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// 移除用户连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveUserConnection(string userId)
    {
        CheckDisposed();

        if (string.IsNullOrEmpty(userId))
            return false;

        if (_userConnections.TryRemove(userId, out var connection))
        {
            RemoveUserConnectionInternal(connection);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 移除端点连接
    /// </summary>
    /// <param name="endpoint">端点</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveEndpointConnection(IPEndPoint endpoint)
    {
        CheckDisposed();

        if (endpoint == null)
            return false;

        if (_endpointConnections.TryRemove(endpoint, out var connection))
        {
            RemoveUserConnectionInternal(connection);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取用户连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户连接，如果不存在则返回null</returns>
    public UserConnection? GetUserConnection(string userId)
    {
        CheckDisposed();
        return _userConnections.TryGetValue(userId, out var connection) ? connection : null;
    }

    /// <summary>
    /// 获取端点连接
    /// </summary>
    /// <param name="endpoint">端点</param>
    /// <returns>用户连接，如果不存在则返回null</returns>
    public UserConnection? GetEndpointConnection(IPEndPoint endpoint)
    {
        CheckDisposed();
        return _endpointConnections.TryGetValue(endpoint, out var connection) ? connection : null;
    }

    /// <summary>
    /// 获取所有用户连接
    /// </summary>
    /// <returns>所有用户连接的副本</returns>
    public IEnumerable<UserConnection> GetAllConnections()
    {
        CheckDisposed();
        return _userConnections.Values.ToArray();
    }

    /// <summary>
    /// 获取所有用户ID
    /// </summary>
    /// <returns>所有用户ID</returns>
    public IEnumerable<string> GetAllUserIds()
    {
        CheckDisposed();
        return _userConnections.Keys.ToArray();
    }

    /// <summary>
    /// 检查用户是否已连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否已连接</returns>
    public bool IsUserConnected(string userId)
    {
        CheckDisposed();
        return _userConnections.ContainsKey(userId);
    }

    /// <summary>
    /// 检查端点是否已连接
    /// </summary>
    /// <param name="endpoint">端点</param>
    /// <returns>是否已连接</returns>
    public bool IsEndpointConnected(IPEndPoint endpoint)
    {
        CheckDisposed();
        return _endpointConnections.ContainsKey(endpoint);
    }

    /// <summary>
    /// 清理断开的连接
    /// </summary>
    public void CleanupDisconnectedConnections()
    {
        CheckDisposed();

        var disconnectedConnections = _userConnections.Values
            .Where(conn => !conn.IsConnected)
            .ToList();

        foreach (var connection in disconnectedConnections)
        {
            RemoveUserConnectionInternal(connection);
        }
    }

    /// <summary>
    /// 获取连接池统计信息
    /// </summary>
    /// <returns>连接池统计信息</returns>
    public NetworkPoolStatistics GetStatistics()
    {
        CheckDisposed();

        return new NetworkPoolStatistics
        {
            MaxConnections = MaxConnections,
            CurrentConnections = CurrentConnections,
            AvailableConnections = AvailableConnections,
            IsFull = IsFull,
            IsEmpty = IsEmpty,
            ActiveConnections = _userConnections.Values.Count(conn => conn.IsConnected),
            DisconnectedConnections = _userConnections.Values.Count(conn => !conn.IsConnected)
        };
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
        return await _connectionSemaphore.WaitAsync(timeout, cancellationToken);
    }

    private void RemoveUserConnectionInternal(UserConnection connection)
    {
        if (connection == null) return;

        // 移除连接事件处理
        connection.ConnectionClosed -= OnConnectionClosed;

        // 从两个字典中移除
        _userConnections.TryRemove(connection.UserInfo.UserId, out _);
        _endpointConnections.TryRemove(connection.RemoteEndPoint, out _);

        // 释放连接槽位
        _connectionSemaphore.Release();

        // 关闭套接字
        try
        {
            connection.Socket?.Close();
        }
        catch (Exception)
        {
            // 忽略关闭套接字时的异常
        }
    }

    private void OnConnectionClosed(object? sender, EventArgs e)
    {
        if (sender is UserConnection connection)
        {
            RemoveUserConnectionInternal(connection);
        }
    }

    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NetworkPool));
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        // 关闭所有连接
        var allConnections = _userConnections.Values.ToArray();
        foreach (var connection in allConnections)
        {
            RemoveUserConnectionInternal(connection);
        }

        _connectionSemaphore?.Dispose();
    }
}

/// <summary>
/// 连接统计事件参数
/// </summary>
public class ConnectionStatisticsEventArgs : EventArgs
{
    /// <summary>
    /// 连接统计信息
    /// </summary>
    public NetworkPoolStatistics Statistics { get; }

    public ConnectionStatisticsEventArgs(NetworkPoolStatistics statistics)
    {
        Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
    }
}
