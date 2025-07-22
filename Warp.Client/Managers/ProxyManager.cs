using System.Net;
using System.Net.Sockets;
using Warp.Client.Interfaces;
using Warp.Client.Models;
using Wrap.Shared.Network.Packets;

namespace Warp.Client.Managers;

/// <summary>
/// 房主端代理管理器
/// </summary>
public class ProxyManager : IDisposable {
    private readonly ProxyConnectionMappingManager _connectionMapping;
    private readonly IClient _client;
    private readonly Timer _cleanupTimer;
    private readonly Dictionary<string, ProxyConnectionInfo> _connections = [];

    private bool _disposed = false;

    // 代理配置
    public string DefaultTargetAddress { get; set; } = "127.0.0.1";
    public int DefaultTargetPort { get; set; } = 80;
    public int MaxConnections { get; set; } = 100;
    public int ConnectionTimeout { get; set; } = 300; // 5分钟

    // 事件
    public event EventHandler<string>? ProxyConnectionEstablished;
    public event EventHandler<string>? ProxyConnectionClosed;
    public event EventHandler<(string connectionId, long bytesTransferred)>? ProxyDataTransferred;

    public ProxyManager(IClient client) {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _connectionMapping = new(_client.Logger);
        _cleanupTimer = new Timer(CleanupExpiredConnections, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// 处理代理连接请求
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功建立连接</returns>
    public async Task<bool> HandleProxyConnectRequestAsync(string connectionId, string userId) {
        try {
            // 检查连接数量限制
            if (_connections.Count >= MaxConnections) {
                return false;
            }

            // 检查连接是否已存在
            if (_connections.ContainsKey(connectionId)) {
                return false;
            }

            // 创建到目标服务器的连接
            var targetClient = new TcpClient();

            var targetEndPoint = new IPEndPoint(IPAddress.Parse(_client.Profile.ProxyTargetAddress), _client.Profile.ProxyTargetPort);
            await targetClient.ConnectAsync(targetEndPoint);

            var connectionInfo = new ProxyConnectionInfo {
                ConnectionId = connectionId,
                TargetClient = targetClient,
                TargetStream = targetClient.GetStream(),
                TargetAddress = targetEndPoint.Address.ToString(),
                TargetPort = targetEndPoint.Port
            };

            _connections[connectionId] = connectionInfo;

            // 添加连接映射
            _connectionMapping.AddMapping(connectionId, userId);

            // 启动同步转发线程
            StartSyncForwarding(connectionInfo);

            ProxyConnectionEstablished?.Invoke(this, connectionId);

            return true;
        } catch (Exception) {
            return false;
        }
    }

    /// <summary>
    /// 处理代理数据
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="data">数据</param>
    /// <param name="isClientToServer">数据方向</param>
    public async Task HandleProxyDataAsync(string connectionId, ProxyDataPacket packet) {
        if (!_connections.TryGetValue(connectionId, out var connectionInfo)) {
            return;
        }

        if (!connectionInfo.IsConnected) {
            return;
        }

        try {
            connectionInfo.LastActivity = DateTime.UtcNow;

            // peer->目标服务器方向：入队，由专用线程写入
            connectionInfo.PeerToTargetQueue.Enqueue(packet);

            // 统计
            ProxyDataTransferred?.Invoke(this, (connectionId, packet.Data.Length));
            _connectionMapping.UpdateActivity(connectionId);
            _connectionMapping.UpdateBytesTransferred(connectionId, packet.Data.Length);
        } catch (Exception ex) {
            await CloseProxyConnectionAsync(connectionId, "数据传输错误");
        }
    }

    /// <summary>
    /// 关闭代理连接
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="reason">关闭原因</param>
    public async Task CloseProxyConnectionAsync(string connectionId, string reason = "") {
        if (!_connections.TryGetValue(connectionId, out var connectionInfo)) {
            return;
        }

        // 先从字典中移除连接，防止重复关闭
        _connections.Remove(connectionId);
        _connectionMapping.RemoveMapping(connectionId);

        try {
            // 关闭peer->目标服务器写入线程
            connectionInfo.PeerToTargetCts.Cancel();
            if (connectionInfo.PeerToTargetTask != null) {
                try { await connectionInfo.PeerToTargetTask; } catch { }
            }
            // 关闭网络连接
            connectionInfo.TargetStream?.Close();
            connectionInfo.TargetClient?.Close();
        } catch (Exception) {
        } finally {
            ProxyConnectionClosed?.Invoke(this, connectionId);
        }
    }

    /// <summary>
    /// 从目标服务器转发数据到客户端
    /// </summary>
    /// <param name="connectionInfo">连接信息</param>
    private void ForwardDataFromTargetSync(ProxyConnectionInfo connectionInfo) {
        var buffer = new byte[65536];
        int readCount = 0;
        while (connectionInfo.IsConnected && !_disposed) {
            if (connectionInfo.TargetStream == null) break;
            int bytesRead = 0;
            try {
                bytesRead = connectionInfo.TargetStream.Read(buffer, 0, buffer.Length);
            } catch (Exception ex) {
                break;
            }
            if (bytesRead == 0) {
                CloseProxyConnectionSync(connectionInfo.ConnectionId, "目标服务器流断开");
                break;
            }
            readCount++;
            // 立即向peer发包（同步）
            var userId = _connectionMapping.GetUserId(connectionInfo.ConnectionId);
            if (!string.IsNullOrEmpty(userId)) {
                var seq = ++connectionInfo.SequenceIdCounter;
                var packet = new ProxyDataPacket(connectionInfo.ConnectionId, buffer.Take(bytesRead).ToArray(), false, seq);
                try {
                    _client.PeerConnectionManager.SendPacket(userId, packet);
                } catch (Exception) {
                }
            }
        }
    }

    // 同步PeerToTargetWriter
    private void PeerToTargetWriterSync(ProxyConnectionInfo connectionInfo) {
        long lastSeq = 0;
        var buffer = new SortedDictionary<long, (byte[] Data, bool IsClientToServer, long Seq)>();
        const int MAX_BUFFER_SIZE = 10000;
        while (!connectionInfo.PeerToTargetCts.IsCancellationRequested && connectionInfo.IsConnected) {
            if (connectionInfo.PeerToTargetQueue.TryDequeue(out var packet)) {
                long seq = packet.SequenceId;
                buffer[seq] = (packet.Data, packet.IsClientToServer, seq);
                if (buffer.Count > MAX_BUFFER_SIZE) {
                    CloseProxyConnectionSync(connectionInfo.ConnectionId, "乱序缓存溢出");
                    return;
                }
            }
            // 顺序写入
            while (buffer.TryGetValue(lastSeq + 1, out var next)) {
                buffer.Remove(lastSeq + 1);
                if (connectionInfo.TargetStream != null) {
                    connectionInfo.WriteLock.Wait();
                    try {
                        connectionInfo.TargetStream.Write(next.Data, 0, next.Data.Length);
                        connectionInfo.TargetStream.Flush();
                    } finally {
                        connectionInfo.WriteLock.Release();
                    }
                }
                lastSeq++;
            }
            Thread.Sleep(1);
        }
    }

    // 同步关闭连接
    private void CloseProxyConnectionSync(string connectionId, string reason) {
        if (_connections.TryGetValue(connectionId, out var connectionInfo)) {
            try {
                connectionInfo.PeerToTargetCts.Cancel();
                connectionInfo.TargetStream?.Close();
                connectionInfo.TargetClient?.Close();
            } catch { }
            _connections.Remove(connectionId);
            _connectionMapping.RemoveMapping(connectionId);
        }
    }

    /// <summary>
    /// 清理过期的连接
    /// </summary>
    private void CleanupExpiredConnections(object? state) {
        if (_disposed) return;

        var expiredConnections = new List<string>();
        var now = DateTime.UtcNow;

        foreach (var kvp in _connections) {
            var connectionId = kvp.Key;
            var connectionInfo = kvp.Value;

            // 检查连接超时
            if ((now - connectionInfo.LastActivity).TotalSeconds > ConnectionTimeout) {
                expiredConnections.Add(connectionId);
            }
        }

        foreach (var connectionId in expiredConnections) {
            _ = CloseProxyConnectionAsync(connectionId, "连接超时");
        }

        // 同时清理映射中的过期连接
        _connectionMapping.CleanupExpiredMappings(TimeSpan.FromSeconds(ConnectionTimeout));
    }

    /// <summary>
    /// 获取代理连接统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public ProxyManagerStats GetProxyStatistics() {
        var connectionStats = new ProxyConnectionStats {
            TotalConnections = _connections.Count,
            ActiveConnections = _connections.Values.Count(c => c.IsConnected),
            MaxConnections = MaxConnections,
            ConnectionTimeout = ConnectionTimeout
        };

        var proxyMappingStats = _connectionMapping.GetMappingStatistics();

        return new ProxyManagerStats {
            DefaultTargetAddress = DefaultTargetAddress,
            DefaultTargetPort = DefaultTargetPort,
            ConnectionStats = connectionStats,
            MappingStats = proxyMappingStats
        };
    }

    /// <summary>
    /// 获取所有代理连接信息
    /// </summary>
    /// <returns>连接信息列表</returns>
    public List<ProxyConnectionInfo> GetAllConnections() {
        return _connections.Values.ToList();
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

    public void Dispose() {
        if (_disposed) return;

        _disposed = true;

        _cleanupTimer?.Dispose();

        // 关闭所有连接
        var connectionIds = _connections.Keys.ToList();

        foreach (var connectionId in connectionIds) {
            _ = CloseProxyConnectionAsync(connectionId, "代理管理器关闭");
        }

        // 等待一小段时间让连接关闭完成
        Thread.Sleep(100);

        // 清理映射
        _connectionMapping?.Dispose();
    }

    // 启动同步转发线程
    public void StartSyncForwarding(ProxyConnectionInfo connectionInfo) {
        new Thread(() => ForwardDataFromTargetSync(connectionInfo)) { IsBackground = true }.Start();
        new Thread(() => PeerToTargetWriterSync(connectionInfo)) { IsBackground = true }.Start();
    }
}