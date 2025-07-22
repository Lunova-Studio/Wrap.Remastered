using System.Net;
using System.Net.Sockets;
using Warp.Client.Interfaces;
using Warp.Client.Managers;
using Warp.Client.Models;
using Wrap.Shared.Network.Packets;

namespace Warp.Client;

/// <summary>
/// 本地代理服务器
/// </summary>
public sealed class LocalProxyServer : IDisposable {
    private readonly IClient _client;
    private readonly TcpListener? _listener;
    private readonly Dictionary<string, LocalProxyConnectionInfo> _connections = [];
    private readonly ProxyConnectionMappingManager _connectionMapping;
    private readonly Timer _cleanupTimer;
    private bool _disposed = false;
    private bool _isRunning = false;

    // 配置
    public int LocalPort { get; set; } = 25565;
    public string TargetAddress { get; set; } = "127.0.0.1";
    public int TargetPort { get; set; } = 25565;
    public int MaxConnections { get; set; } = 10;
    public int ConnectionTimeout { get; set; } = 300; // 5分钟

    // 事件
    public event EventHandler<string>? LocalConnectionEstablished;
    public event EventHandler<string>? LocalConnectionClosed;
    public event EventHandler<(string connectionId, long bytesTransferred)>? LocalDataTransferred;

    public LocalProxyServer(IClient client, int localPort, string targetAddress, int targetPort) {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        LocalPort = localPort;
        TargetAddress = targetAddress;
        TargetPort = targetPort;

        try {
            _connectionMapping = new(_client.Logger);
            _listener = new TcpListener(IPAddress.Any, LocalPort);
        } catch (Exception) {
            throw;
        }

        // 启动清理定时器（每30秒检查一次）
        _cleanupTimer = new Timer(CleanupExpiredConnections, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// 启动本地代理服务器
    /// </summary>
    public async Task StartAsync() {
        if (_disposed || _listener == null) {
            throw new InvalidOperationException("代理服务器已关闭或未初始化");
        }

        try {
            _listener.Start();
            _isRunning = true;
            _ = Task.Run(() => ListenForConnectionsAsync());
        } catch (Exception) {
            throw;
        }
    }

    /// <summary>
    /// 停止本地代理服务器
    /// </summary>
    public async Task StopAsync() {
        if (!_isRunning) return;

        _isRunning = false;

        try {
            _listener?.Stop();
        } catch (Exception) { }

        // 关闭所有连接
        var connectionIds = _connections.Keys.ToList();
        foreach (var connectionId in connectionIds) {
            await CloseLocalConnectionAsync(connectionId, "服务器停止");
        }
    }

    /// <summary>
    /// 监听本地连接
    /// </summary>
    private async Task ListenForConnectionsAsync() {
        while (_isRunning && !_disposed) {
            try {
                var client = await _listener!.AcceptTcpClientAsync();
                await HandleLocalConnectionAsync(client);
            } catch (Exception) {

            }
        }
    }

    /// <summary>
    /// 处理本地连接
    /// </summary>
    /// <param name="client">本地TCP客户端</param>
    private async Task HandleLocalConnectionAsync(TcpClient client) {
        var connectionId = Guid.NewGuid().ToString();

        try {
            // 检查连接数量限制
            if (_connections.Count >= MaxConnections) {
                client.Close();
                return;
            }

            var connectionInfo = new LocalProxyConnectionInfo {
                ConnectionId = connectionId,
                LocalClient = client,
                LocalStream = client.GetStream()
            };

            _connections[connectionId] = connectionInfo;

            // 添加连接映射（这里暂时使用连接ID作为用户ID，实际应该从房主信息获取）
            // 在实际应用中，需要从房间信息中获取房主ID
            var ownerUserId = GetOwnerUserId();
            if (!string.IsNullOrEmpty(ownerUserId)) {
                _connectionMapping.AddMapping(connectionId, ownerUserId);
            }

            // 向房主发送代理连接请求
            var connectPacket = new ProxyConnectPacket(connectionId);
            await _client.SendPeerPacketAsync(ownerUserId ?? connectionId, connectPacket);

            // 启动同步转发线程
            StartSyncForwarding(connectionInfo);

            LocalConnectionEstablished?.Invoke(this, connectionId);
        } catch (Exception) {
            client.Close();
        }
    }

    /// <summary>
    /// 处理代理连接响应
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="success">是否成功</param>
    /// <param name="errorMessage">错误信息</param>
    public async Task HandleProxyResponseAsync(string connectionId, bool success, string errorMessage) {
        if (!_connections.TryGetValue(connectionId, out var connectionInfo)) {
            return;
        }

        if (success) {
            // 设置连接状态为活跃
            _connectionMapping.SetConnectionStatus(connectionId, true);
        } else {
            // 设置连接状态为非活跃
            _connectionMapping.SetConnectionStatus(connectionId, false);
            await CloseLocalConnectionAsync(connectionId, "连接建立失败");
        }
    }

    // 同步从本地客户端转发数据到房主
    private void ForwardDataFromLocal(LocalProxyConnectionInfo connectionInfo) {
        var buffer = new byte[65536];
        while (connectionInfo.IsConnected && !_disposed) {
            if (connectionInfo.LocalStream == null) break;
            int bytesRead = 0;

            try {
                bytesRead = connectionInfo.LocalStream.Read(buffer, 0, buffer.Length);
            } catch (Exception) {
                break;
            }

            if (bytesRead == 0) {
                Thread.Sleep(1);
                continue;
            }

            var ownerUserId = _connectionMapping.GetUserId(connectionInfo.ConnectionId);
            if (!string.IsNullOrEmpty(ownerUserId)) {
                var seq = System.Threading.Interlocked.Increment(ref connectionInfo.SequenceIdCounter);
                var dataPacket = new ProxyDataPacket(connectionInfo.ConnectionId, [.. buffer.Take(bytesRead)], true, seq);
                _client.PeerConnectionManager.SendPacket(ownerUserId, dataPacket);
            }
        }
    }

    // 同步处理P2P->本地的数据
    public void HandleProxyData(string connectionId, ProxyDataPacket packet) {
        byte[] data = packet.Data;
        if (!_connections.TryGetValue(connectionId, out var connectionInfo)) {
            return;
        }

        if (!connectionInfo.IsConnected) {
            return;
        }

        try {
            connectionInfo.LastActivity = DateTime.UtcNow;
            if (connectionInfo.LocalStream != null) {
                connectionInfo.LocalStream.Write(data, 0, data.Length);
                connectionInfo.LocalStream.Flush();

                LocalDataTransferred?.Invoke(this, (connectionId, data.Length));
                _connectionMapping.UpdateActivity(connectionInfo.ConnectionId);
                _connectionMapping.UpdateBytesTransferred(connectionInfo.ConnectionId, data.Length);
            }
        } catch (Exception) {
            // 关闭连接
            _ = CloseLocalConnectionAsync(connectionId, "数据传输错误");
        }
    }

    // 启动同步转发线程
    public void StartSyncForwarding(LocalProxyConnectionInfo connectionInfo) {
        new Thread(() => ForwardDataFromLocal(connectionInfo)) { IsBackground = true }.Start();
    }

    /// <summary>
    /// 关闭本地连接
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="reason">关闭原因</param>
    public async Task CloseLocalConnectionAsync(string connectionId, string reason = "") {
        if (!_connections.TryGetValue(connectionId, out var connectionInfo)) {
            return;
        }

        // 先从字典中移除连接，防止重复关闭
        _connections.Remove(connectionId);
        _connectionMapping.RemoveMapping(connectionId);

        try {
            // 关闭网络连接
            connectionInfo.LocalStream?.Close();
            connectionInfo.LocalClient?.Close();
        } catch (Exception) {

        } finally {
            LocalConnectionClosed?.Invoke(this, connectionId);
        }
    }

    /// <summary>
    /// 清理过期的连接
    /// </summary>
    private void CleanupExpiredConnections(object? state) {
        if (_disposed)
            return;

        var expiredConnections = new List<string>();
        var now = DateTime.UtcNow;

        foreach (var kvp in _connections) {
            var connectionId = kvp.Key;
            var connectionInfo = kvp.Value;

            // 检查连接超时
            if ((now - connectionInfo.LastActivity).TotalSeconds > ConnectionTimeout)
                expiredConnections.Add(connectionId);
        }

        foreach (var connectionId in expiredConnections)
            _ = CloseLocalConnectionAsync(connectionId, "连接超时");

        // 同时清理映射中的过期连接
        _connectionMapping.CleanupExpiredMappings(TimeSpan.FromSeconds(ConnectionTimeout));
    }

    /// <summary>
    /// 获取本地代理统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public LocalProxyStats GetLocalProxyStatistics() {
        var connectionStats = new ProxyConnectionStats {
            TotalConnections = _connections.Count,
            ActiveConnections = _connections.Values.Count(c => c.IsConnected),
            MaxConnections = MaxConnections,
            ConnectionTimeout = ConnectionTimeout
        };

        var proxyMappingStats = _connectionMapping.GetMappingStatistics();

        return new LocalProxyStats {
            IsRunning = _isRunning,
            LocalPort = LocalPort,
            TargetAddress = TargetAddress,
            TargetPort = TargetPort,
            ConnectionStats = connectionStats,
            MappingStats = proxyMappingStats
        };
    }

    /// <summary>
    /// 获取所有本地连接信息
    /// </summary>
    /// <returns>连接信息列表</returns>
    public List<LocalProxyConnectionInfo> GetAllConnections() {
        return [.. _connections.Values];
    }

    /// <summary>
    /// 获取所有连接映射信息
    /// </summary>
    /// <returns>映射信息列表</returns>
    public List<ProxyConnectionMappingInfo> GetAllMappings() {
        return _connectionMapping.GetAllMappings();
    }

    /// <summary>
    /// 获取活跃连接映射信息
    /// </summary>
    /// <returns>活跃映射信息列表</returns>
    public List<ProxyConnectionMappingInfo> GetActiveMappings() {
        return _connectionMapping.GetActiveMappings();
    }

    /// <summary>
    /// 获取用户的所有连接映射信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>映射信息列表</returns>
    public List<ProxyConnectionMappingInfo> GetUserMappings(string userId) {
        return _connectionMapping.GetUserMappings(userId);
    }

    /// <summary>
    /// 根据连接ID获取用户ID
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <returns>用户ID</returns>
    public string? GetUserId(string connectionId) {
        return _connectionMapping.GetUserId(connectionId);
    }

    /// <summary>
    /// 根据用户ID获取连接ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>连接ID列表</returns>
    public List<string> GetConnectionIds(string userId) {
        return _connectionMapping.GetConnectionIds(userId);
    }

    /// <summary>
    /// 获取房主用户ID
    /// </summary>
    /// <returns>房主用户ID</returns>
    private string? GetOwnerUserId() {
        if (_client.CurrentRoomInfo != null) {
            return _client.CurrentRoomInfo.Owner.UserId;
        }
        return null;
    }

    public void Dispose() {
        if (_disposed) return;

        _disposed = true;

        _cleanupTimer?.Dispose();

        // 关闭所有连接
        var connectionIds = _connections.Keys.ToList();

        foreach (var connectionId in connectionIds)
            _ = CloseLocalConnectionAsync(connectionId, "本地代理服务器关闭");

        // 停止服务器
        _ = StopAsync();

        // 等待一小段时间让连接关闭完成
        Thread.Sleep(100);

        // 清理映射
        _connectionMapping?.Dispose();
    }
}