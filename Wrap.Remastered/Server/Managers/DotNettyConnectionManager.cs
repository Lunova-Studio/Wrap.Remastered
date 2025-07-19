using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Server.Handlers;
using Wrap.Remastered.Server.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Wrap.Remastered.Server.Managers;

/// <summary>
/// DotNetty连接管理器
/// </summary>
public class DotNettyConnectionManager : IConnectionManager, IDisposable
{
    private readonly ConcurrentDictionary<IChannel, ChannelConnection> _connections;
    private readonly ConcurrentDictionary<string, ChannelConnection> _userConnections;
    private readonly Timer _cleanupTimer;
    private volatile bool _disposed = false;
    private readonly IWrapServer _server;

    /// <summary>
    /// 连接事件
    /// </summary>
    public event EventHandler<ChannelConnectionEventArgs>? ClientConnected;
    public event EventHandler<ChannelConnectionEventArgs>? ClientDisconnected;
    public event EventHandler<ChannelDataEventArgs>? DataReceived;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="cleanupInterval">清理间隔（毫秒）</param>
    public DotNettyConnectionManager(IWrapServer server, int cleanupInterval = 30000)
    {
        _server = server;
        _connections = new ConcurrentDictionary<IChannel, ChannelConnection>();
        _userConnections = new ConcurrentDictionary<string, ChannelConnection>();
        
        // 启动定期清理任务
        _cleanupTimer = new Timer(CleanupInactiveConnections, null, cleanupInterval, cleanupInterval);
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
    public void OnClientConnected(IChannel channel)
    {
        CheckDisposed();

        if (channel == null)
            return;

        var connection = new ChannelConnection(channel);
        
        if (_connections.TryAdd(channel, connection))
        {
            _server.GetLoggingService().LogConnection("新客户端连接: {0}", channel.RemoteAddress);
            ClientConnected?.Invoke(this, new ChannelConnectionEventArgs(connection));
        }
    }

    /// <summary>
    /// 客户端断开时调用
    /// </summary>
    /// <param name="channel">通道</param>
    public void OnClientDisconnected(IChannel channel)
    {
        if (_disposed || channel == null)
            return;

        if (_connections.TryRemove(channel, out var connection))
        {
            _server.GetLoggingService().LogConnection("客户端断开: {0}", channel.RemoteAddress);
            
            // 如果连接有用户信息，也从用户连接字典中移除
            if (!string.IsNullOrEmpty(connection.UserId))
            {
                _userConnections.TryRemove(connection.UserId, out _);
            }
            
            ClientDisconnected?.Invoke(this, new ChannelConnectionEventArgs(connection));
        }
    }

    /// <summary>
    /// 更新连接活动时间
    /// </summary>
    /// <param name="channel">通道</param>
    public void UpdateConnectionActivity(IChannel channel)
    {
        if (_disposed || channel == null)
            return;

        if (_connections.TryGetValue(channel, out var connection))
        {
            connection.UpdateActivity();
        }
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="userInfo">用户信息</param>
    public void SetUserInfo(IChannel channel, UserInfo userInfo)
    {
        CheckDisposed();

        if (channel == null || userInfo == null)
            return;

        if (_connections.TryGetValue(channel, out var connection))
        {
            connection.SetUserInfo(userInfo);
            
            // 添加到用户连接字典
            _userConnections.TryAdd(userInfo.UserId, connection);
            
            _server.GetLoggingService().LogUser("用户 {0} 已关联到通道 {1}", userInfo.UserId, channel.RemoteAddress);
        }
    }

    /// <summary>
    /// 获取用户连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>通道连接</returns>
    public ChannelConnection? GetUserConnection(string userId)
    {
        CheckDisposed();
        return _userConnections.TryGetValue(userId, out var connection) ? connection : null;
    }

    /// <summary>
    /// 获取通道连接
    /// </summary>
    /// <param name="channel">通道</param>
    /// <returns>通道连接</returns>
    public ChannelConnection? GetChannelConnection(IChannel channel)
    {
        CheckDisposed();
        return _connections.TryGetValue(channel, out var connection) ? connection : null;
    }

    /// <summary>
    /// 获取所有连接
    /// </summary>
    /// <returns>所有连接</returns>
    public IEnumerable<ChannelConnection> GetAllConnections()
    {
        CheckDisposed();
        return _connections.Values.ToArray();
    }

    /// <summary>
    /// 获取所有用户连接
    /// </summary>
    /// <returns>所有用户连接</returns>
    public IEnumerable<ChannelConnection> GetAllUserConnections()
    {
        CheckDisposed();
        return _userConnections.Values.ToArray();
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
    /// 发送数据给用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="data">数据</param>
    /// <returns>是否成功发送</returns>
    public async Task<bool> SendDataToUserAsync(string userId, byte[] data)
    {
        CheckDisposed();

        var connection = GetUserConnection(userId);
        if (connection == null || !connection.IsActive)
            return false;

        return await connection.SendDataAsync(data);
    }

    /// <summary>
    /// 发送数据给通道
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="data">数据</param>
    /// <returns>是否成功发送</returns>
    public async Task<bool> SendDataToChannelAsync(IChannel channel, byte[] data)
    {
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
    public async Task<int> BroadcastToUsersAsync(byte[] data, string? excludeUserId = null)
    {
        CheckDisposed();

        var connections = _userConnections.Values
            .Where(conn => conn.IsActive && (excludeUserId == null || conn.UserId != excludeUserId))
            .ToList();

        var tasks = connections.Select(async connection =>
        {
            try
            {
                return await connection.SendDataAsync(data);
            }
            catch (Exception)
            {
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
    public async Task<int> BroadcastToAllAsync(byte[] data)
    {
        CheckDisposed();

        var connections = _connections.Values
            .Where(conn => conn.IsActive)
            .ToList();

        var tasks = connections.Select(async connection =>
        {
            try
            {
                return await connection.SendDataAsync(data);
            }
            catch (Exception)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Count(x => x);
    }
    public async Task<int> BroadcastToAllAsync(IClientBoundPacket packet, string? excludeUserId = null)
    {
        CheckDisposed();

        var connections = _connections.Values
            .Where(conn => conn.IsActive)
            .ToList();

        var tasks = connections.Select(async connection =>
        {
            try
            {
                return await connection.SendPacketAsync(packet);
            }
            catch (Exception)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Count(x => x);
    }

    public async Task<int> BroadcastToUsersAsync(IClientBoundPacket packet, string? excludeUserId = null)
    {
        CheckDisposed();

        var connections = _userConnections.Values
            .Where(conn => conn.IsActive && (excludeUserId == null || conn.UserId != excludeUserId))
            .ToList();

        var tasks = connections.Select(async connection =>
        {
            try
            {
                return await connection.SendPacketAsync(packet);
            }
            catch (Exception)
            {
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
    public bool DisconnectUser(string userId, string? reason = null)
    {
        CheckDisposed();

        if (string.IsNullOrEmpty(userId))
            return false;

        if (_userConnections.TryRemove(userId, out var connection))
        {
            connection.SendPacketAsync(new DisconnectPacket(reason)).Wait();
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
    public bool DisconnectChannel(IChannel channel)
    {
        CheckDisposed();

        if (channel == null)
            return false;

        if (_connections.TryRemove(channel, out var connection))
        {
            connection.Close();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    /// <returns>连接统计信息</returns>
    public ConnectionStatistics GetStatistics()
    {
        if (_disposed)
            return new ConnectionStatistics();

        var totalConnections = _connections.Count;
        var userConnections = _userConnections.Count;
        var activeConnections = _connections.Values.Count(conn => conn.IsActive);
        var inactiveConnections = totalConnections - activeConnections;

        return new ConnectionStatistics
        {
            TotalConnections = totalConnections,
            UserConnections = userConnections,
            ActiveConnections = activeConnections,
            InactiveConnections = inactiveConnections
        };
    }

    /// <summary>
    /// 发送IClientBoundPacket给指定用户
    /// </summary>
    public async Task<bool> SendPacketToUserAsync(string userId, IClientBoundPacket packet)
    {
        CheckDisposed();
        var connection = GetUserConnection(userId);
        if (connection == null || !connection.IsActive)
            return false;
        return await connection.SendPacketAsync(packet);
    }

    /// <summary>
    /// 清理非活跃连接
    /// </summary>
    private void CleanupInactiveConnections(object? state)
    {
        try
        {
            // 如果已释放，不执行清理
            if (_disposed)
                return;

            var inactiveConnections = _connections.Values
                .Where(conn => !conn.IsActive)
                .ToList();

            foreach (var connection in inactiveConnections)
            {
                _connections.TryRemove(connection.Channel, out _);
                if (!string.IsNullOrEmpty(connection.UserId))
                {
                    _userConnections.TryRemove(connection.UserId, out _);
                }
            }

            if (inactiveConnections.Count > 0)
            {
                _server.GetLoggingService().LogConnection("清理了 {0} 个非活跃连接", inactiveConnections.Count);
            }
        }
        catch (Exception ex)
        {
            _server.GetLoggingService().LogError("Connection", "清理连接时发生错误", ex);
        }
    }

    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DotNettyConnectionManager));
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        // 停止定时器
        _cleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _cleanupTimer?.Dispose();

        // 关闭所有连接（异步执行，不等待完成）
        var allConnections = _connections.Values.ToArray();
        foreach (var connection in allConnections)
        {
            try
            {
                connection.Close();
            }
            catch (Exception)
            {
                // 忽略关闭连接时的错误
            }
        }

        _connections.Clear();
        _userConnections.Clear();
    }
}

/// <summary>
/// 通道连接
/// </summary>
public class ChannelConnection
{
    private UserInfo? _userInfo;
    private DateTime _lastActivity;

    /// <summary>
    /// 通道
    /// </summary>
    public IChannel Channel { get; }

    /// <summary>
    /// 连接时间
    /// </summary>
    public DateTime ConnectedAt { get; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivity
    {
        get => _lastActivity;
        private set => _lastActivity = value;
    }

    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfo? UserInfo => _userInfo;

    /// <summary>
    /// 用户ID
    /// </summary>
    public string? UserId => _userInfo?.UserId;

    /// <summary>
    /// 是否活跃
    /// </summary>
    public bool IsActive => Channel.Active;

    /// <summary>
    /// 远程地址
    /// </summary>
    public string RemoteAddress => Channel.RemoteAddress?.ToString() ?? "Unknown";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="channel">通道</param>
    public ChannelConnection(IChannel channel)
    {
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        ConnectedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    public void SetUserInfo(UserInfo userInfo)
    {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        UpdateActivity();
    }

    /// <summary>
    /// 更新活动时间
    /// </summary>
    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>是否成功发送</returns>
    public async Task<bool> SendDataAsync(byte[] data)
    {
        if (!IsActive || data == null || data.Length == 0)
            return false;

        try
        {
            var buffer = Unpooled.WrappedBuffer(data);
            await Channel.WriteAndFlushAsync(buffer);
            UpdateActivity();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> SendPacketAsync(IClientBoundPacket packet)
    {
        try
        {
            var serializer = packet.GetSerializer();
            var packetData = serializer.Serialize(packet);
            packet.OnSerialize(ref packetData);

            using MemoryStream stream = new MemoryStream(4 + packetData.Length);
            stream.WriteInt32((int)packet.GetPacketType());
            await stream.WriteAsync(packetData, 0, packetData.Length);

            var buffer = DotNetty.Buffers.Unpooled.WrappedBuffer(stream.GetBuffer());
            await Channel.WriteAndFlushAsync(buffer);

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void Close()
    {
        try
        {
            if (IsActive)
            {
                // 异步关闭，不等待完成
                _ = Channel.CloseAsync();
            }
        }
        catch (Exception)
        {
            // 忽略关闭时的错误
        }
    }
}

/// <summary>
/// 通道连接事件参数
/// </summary>
public class ChannelConnectionEventArgs : EventArgs
{
    /// <summary>
    /// 通道连接
    /// </summary>
    public ChannelConnection Connection { get; }

    public ChannelConnectionEventArgs(ChannelConnection connection)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
}

/// <summary>
/// 通道数据事件参数
/// </summary>
public class ChannelDataEventArgs : EventArgs
{
    /// <summary>
    /// 通道连接
    /// </summary>
    public ChannelConnection Connection { get; }

    /// <summary>
    /// 数据
    /// </summary>
    public byte[] Data { get; }

    public ChannelDataEventArgs(ChannelConnection connection, byte[] data)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }
}

/// <summary>
/// 连接统计信息
/// </summary>
public class ConnectionStatistics
{
    /// <summary>
    /// 总连接数
    /// </summary>
    public int TotalConnections { get; set; }

    /// <summary>
    /// 用户连接数
    /// </summary>
    public int UserConnections { get; set; }

    /// <summary>
    /// 活跃连接数
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// 非活跃连接数
    /// </summary>
    public int InactiveConnections { get; set; }
} 