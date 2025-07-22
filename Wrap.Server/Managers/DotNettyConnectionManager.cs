using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Events;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network;
using Wrap.Shared.Network.Packets;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Remastered.Server.Managers;

/// <summary>
/// DotNetty连接管理器
/// </summary>
public sealed class DotNettyConnectionManager : IConnectionManager, IDisposable {
    private readonly ConcurrentDictionary<IChannel, ChannelConnection> _connections;
    private readonly ConcurrentDictionary<string, ChannelConnection> _userConnections;
    private readonly Timer _cleanupTimer;
    private volatile bool _disposed = false;
    private readonly IServerCoordinator _serverCoordinator;

    public event EventHandler<ChannelConnectionEventArgs>? ClientConnected;
    public event EventHandler<ChannelConnectionEventArgs>? ClientDisconnected;
    public event EventHandler<ChannelDataEventArgs>? DataReceived;

    public DotNettyConnectionManager(IServerCoordinator coordinator, int cleanupInterval = 30000) {
        _serverCoordinator = coordinator;
        _connections = new ConcurrentDictionary<IChannel, ChannelConnection>();
        _userConnections = new ConcurrentDictionary<string, ChannelConnection>();

        // 启动定期清理任务
        _cleanupTimer = new Timer(async state => await CleanupInactiveConnectionsAsync(state), null, cleanupInterval, cleanupInterval);
    }

    /// <summary>
    /// 当前连接数
    /// </summary>
    public int CurrentConnections => _connections.Count;

    /// <summary>
    /// 当前用户连接数
    /// </summary>
    public int CurrentUserConnections => _userConnections.Count;

    /// <summary>
    /// 客户端连接时调用
    /// </summary>
    /// <param name="channel">通道</param>
    public void OnClientConnected(IChannel channel) {
        CheckDisposed();

        if (channel == null)
            return;

        var connection = new ChannelConnection(channel);

        if (_connections.TryAdd(channel, connection)) {
            _serverCoordinator.Logger.LogInformation("新客户端连接: {RemoteAddress}", channel.RemoteAddress);
            ClientConnected?.Invoke(this, new ChannelConnectionEventArgs(connection));
        }
    }

    /// <summary>
    /// 客户端断开时调用
    /// </summary>
    /// <param name="channel">通道</param>
    public void OnClientDisconnected(IChannel channel) {
        if (_disposed || channel == null)
            return;

        if (_connections.TryRemove(channel, out var connection)) {
            _serverCoordinator.Logger.LogInformation("客户端断开: {RemoteAddress}", channel.RemoteAddress);

            // 如果连接有用户信息，也从用户连接字典中移除
            if (!string.IsNullOrEmpty(connection.UserId)) {
                _userConnections.TryRemove(connection.UserId, out _);
            }

            ClientDisconnected?.Invoke(this, new ChannelConnectionEventArgs(connection));
        }
    }

    /// <summary>
    /// 更新连接活动时间
    /// </summary>
    /// <param name="channel">通道</param>
    public void UpdateConnectionActivity(IChannel channel) {
        if (_disposed || channel == null)
            return;

        if (_connections.TryGetValue(channel, out var connection)) {
            connection.UpdateActivity();
        }
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="userInfo">用户信息</param>
    public void SetUserInfo(IChannel channel, UserInfo userInfo) {
        CheckDisposed();

        if (channel == null || userInfo == null)
            return;

        if (_connections.TryGetValue(channel, out var connection)) {
            connection.SetUserInfo(userInfo);

            // 添加到用户连接字典
            _userConnections.TryAdd(userInfo.UserId, connection);
            _serverCoordinator.Logger.LogInformation("用户 {user} 已关联到通道 {RemoteAddress}", userInfo.UserId, channel.RemoteAddress);
        }
    }

    /// <summary>
    /// 获取用户连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>通道连接</returns>
    public ChannelConnection? GetUserConnection(string userId) {
        CheckDisposed();
        return _userConnections.TryGetValue(userId, out var connection) ? connection : null;
    }

    /// <summary>
    /// 获取通道连接
    /// </summary>
    /// <param name="channel">通道</param>
    /// <returns>通道连接</returns>
    public ChannelConnection? GetChannelConnection(IChannel channel) {
        CheckDisposed();
        return _connections.TryGetValue(channel, out var connection) ? connection : null;
    }

    /// <summary>
    /// 获取所有连接
    /// </summary>
    /// <returns>所有连接</returns>
    public IEnumerable<ChannelConnection> GetAllConnections() {
        CheckDisposed();
        return [.. _connections.Values];
    }

    /// <summary>
    /// 获取所有用户连接
    /// </summary>
    /// <returns>所有用户连接</returns>
    public IEnumerable<ChannelConnection> GetAllUserConnections() {
        CheckDisposed();
        return [.. _userConnections.Values];
    }

    /// <summary>
    /// 检查用户是否已连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否已连接</returns>
    public bool IsUserConnected(string userId) {
        CheckDisposed();
        return _userConnections.ContainsKey(userId);
    }

    /// <summary>
    /// 发送数据给用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="data">数据</param>
    /// <returns>是否成功发送</returns>
    public async Task<bool> SendDataToUserAsync(string userId, byte[] data) {
        CheckDisposed();

        var connection = GetUserConnection(userId);
        if (connection == null || !connection.IsActive)
            return false;

        return await connection.SendDataAsync(data);
    }

    public async Task<bool> SendPacketToUserAsync(string userId, IClientBoundPacket packet) {
        CheckDisposed();

        var connection = GetUserConnection(userId);
        if (connection == null || !connection.IsActive)
            return false;

        return await connection.SendPacketAsync(packet);
    }

    /// <summary>
    /// 发送数据给通道
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="data">数据</param>
    /// <returns>是否成功发送</returns>
    public async Task<bool> SendDataToChannelAsync(IChannel channel, byte[] data) {
        CheckDisposed();

        var connection = GetChannelConnection(channel);
        if (connection == null || !connection.IsActive)
            return false;

        return await connection.SendDataAsync(data);
    }

    /// <summary>
    /// 广播数据给所有用户
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <returns>成功发送的连接数</returns>
    public async Task<int> BroadcastToUsersAsync(byte[] data, string? excludeUserId = null) {
        CheckDisposed();

        var connections = _userConnections.Values
            .Where(conn => conn.IsActive && (excludeUserId == null || conn.UserId != excludeUserId))
            .ToList();

        var tasks = connections.Select(async connection => {
            try {
                return await connection.SendDataAsync(data);
            } catch (Exception) {
                return false;
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Count(x => x);
    }

    /// <summary>
    /// 广播数据给所有连接
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>成功发送的连接数</returns>
    public async Task<int> BroadcastToAllAsync(byte[] data) {
        CheckDisposed();

        var connections = _connections.Values
            .Where(conn => conn.IsActive)
            .ToList();

        var tasks = connections.Select(async connection => {
            try {
                return await connection.SendDataAsync(data);
            } catch (Exception) {
                return false;
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Count(x => x);
    }
    public async Task<int> BroadcastToAllAsync(IClientBoundPacket packet) {
        CheckDisposed();

        var connections = _connections.Values
            .Where(conn => conn.IsActive)
            .ToList();

        var tasks = connections.Select(async connection => {
            try {
                return await connection.SendPacketAsync(packet);
            } catch (Exception) {
                return false;
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Count(x => x);
    }

    public async Task<int> BroadcastToUsersAsync(IClientBoundPacket packet, string? excludeUserId = null) {
        CheckDisposed();

        var connections = _userConnections.Values
            .Where(conn => conn.IsActive && (excludeUserId == null || conn.UserId != excludeUserId))
            .ToList();

        var tasks = connections.Select(async connection => {
            try {
                return await connection.SendPacketAsync(packet);
            } catch (Exception) {
                return false;
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Count(x => x);
    }

    /// <summary>
    /// 断开用户连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功断开</returns>
    public async Task<bool> DisconnectUserAsync(string userId, string? reason = null) {
        CheckDisposed();

        if (string.IsNullOrEmpty(userId))
            return false;

        if (_userConnections.TryRemove(userId, out var connection)) {
            await connection.SendPacketAsync(new DisconnectPacket(reason));
            connection.Close();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 断开通道连接
    /// </summary>
    /// <param name="channel">通道</param>
    /// <returns>是否成功断开</returns>
    public bool DisconnectChannel(IChannel channel) {
        CheckDisposed();

        if (channel == null)
            return false;

        if (_connections.TryRemove(channel, out var connection)) {
            connection.Close();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    /// <returns>连接统计信息</returns>
    public ConnectionStatistics GetStatistics() {
        if (_disposed)
            return new ConnectionStatistics();

        var totalConnections = _connections.Count;
        var userConnections = _userConnections.Count;
        var activeConnections = _connections.Values.Count(conn => conn.IsActive);
        var inactiveConnections = totalConnections - activeConnections;

        return new ConnectionStatistics {
            TotalConnections = totalConnections,
            UserConnections = userConnections,
            ActiveConnections = activeConnections,
            InactiveConnections = inactiveConnections
        };
    }

    /// <summary>
    /// 清理非活跃连接
    /// </summary>
    private async Task CleanupInactiveConnectionsAsync(object? state) {
        try {
            // 如果已释放，不执行清理
            if (_disposed)
                return;

            var now = DateTime.UtcNow;
            var inactiveConnections = _connections.Values
                .Where(conn => !conn.IsActive || (now - conn.LastActivity).TotalSeconds > 15) // 15秒无活动则断开
                .ToList();

            foreach (var connection in inactiveConnections) {
                _serverCoordinator.Logger.LogInformation("清理非活跃连接: {RemoteAddress}", connection.RemoteAddress);

                // 先发送断开包告知原因
                var disconnectPacket = new DisconnectPacket("连接超时，无活动");
                await connection.SendPacketAsync(disconnectPacket);

                connection.Close();
                _connections.TryRemove(connection.Channel, out _);

                // 如果连接有用户信息，也从用户连接字典中移除
                if (!string.IsNullOrEmpty(connection.UserId))
                    _userConnections.TryRemove(connection.UserId, out _);
            }
        } catch (Exception ex) {
            _serverCoordinator.Logger.LogError(ex, "清理非活跃连接时发生错误: {ex}", ex);
        }
    }

    private void CheckDisposed() {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose() {
        if (_disposed) return;

        _disposed = true;

        // 停止定时器
        _cleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _cleanupTimer?.Dispose();

        // 关闭所有连接（异步执行，不等待完成）
        var allConnections = _connections.Values.ToArray();
        foreach (var connection in allConnections) {
            try {
                connection.Close();
            } catch (Exception) {
                // 忽略关闭连接时的错误
            }
        }

        _connections.Clear();
        _userConnections.Clear();
    }
}