using ConsoleInteractive;
using System.Net;
using System.Net.Sockets;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.PeerBound;

namespace Wrap.Remastered.Client;

/// <summary>
/// 代理连接信息
/// </summary>
public class ProxyConnectionInfo
{
    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 目标TCP客户端
    /// </summary>
    public TcpClient? TargetClient { get; set; }
    
    /// <summary>
    /// 目标网络流
    /// </summary>
    public NetworkStream? TargetStream { get; set; }
    
    /// <summary>
    /// 目标地址
    /// </summary>
    public string TargetAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 目标端口
    /// </summary>
    public int TargetPort { get; set; }
    
    /// <summary>
    /// 连接状态
    /// </summary>
    public bool IsConnected => TargetClient?.Connected == true;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 房主端代理管理器
/// </summary>
public class ProxyManager : IDisposable
{
    private readonly Dictionary<string, ProxyConnectionInfo> _connections = new();
    private readonly ProxyConnectionMapping _connectionMapping = new();
    private readonly IWrapClient _client;
    private readonly Timer _cleanupTimer;
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

    public ProxyManager(IWrapClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        
        // 启动清理定时器（每30秒检查一次）
        _cleanupTimer = new Timer(CleanupExpiredConnections, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        ConsoleWriter.WriteLine("[代理] 代理管理器已启动");
    }

    /// <summary>
    /// 处理代理连接请求
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功建立连接</returns>
    public async Task<bool> HandleProxyConnectRequestAsync(string connectionId, string userId)
    {
        try
        {
            // 检查连接数量限制
            if (_connections.Count >= MaxConnections)
            {
                ConsoleWriter.WriteLine($"[代理] 连接数量已达上限 ({MaxConnections})，拒绝新连接: {connectionId}");
                return false;
            }
            
            // 检查连接是否已存在
            if (_connections.ContainsKey(connectionId))
            {
                ConsoleWriter.WriteLine($"[代理] 连接ID已存在: {connectionId}");
                return false;
            }
            
            // 创建到目标服务器的连接
            var targetClient = new TcpClient();

            var targetEndPoint = new IPEndPoint(IPAddress.Parse(_client.Profile.ProxyTargetAddress), _client.Profile.ProxyTargetPort);
            await targetClient.ConnectAsync(targetEndPoint);
            
            var connectionInfo = new ProxyConnectionInfo
            {
                ConnectionId = connectionId,
                TargetClient = targetClient,
                TargetStream = targetClient.GetStream(),
                TargetAddress = targetEndPoint.Address.ToString(),
                TargetPort = targetEndPoint.Port
            };
            
            _connections[connectionId] = connectionInfo;
            
            // 添加连接映射
            _connectionMapping.AddMapping(connectionId, userId);
            
            // 启动数据转发任务
            _ = Task.Run(() => ForwardDataFromTargetAsync(connectionInfo));
            
            ProxyConnectionEstablished?.Invoke(this, connectionId);
            
            return true;
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[代理] 建立代理连接失败: {connectionId}, 错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 处理代理数据
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="data">数据</param>
    /// <param name="isClientToServer">数据方向</param>
    public async Task HandleProxyDataAsync(string connectionId, byte[] data, bool isClientToServer)
    {
        if (!_connections.TryGetValue(connectionId, out var connectionInfo))
        {
            ConsoleWriter.WriteLine($"[代理] 连接不存在: {connectionId}");
            return;
        }
        
        if (!connectionInfo.IsConnected)
        {
            ConsoleWriter.WriteLine($"[代理] 连接已断开: {connectionId}");
            return;
        }
        
        try
        {
            connectionInfo.LastActivity = DateTime.UtcNow;
            
            if (isClientToServer)
            {
                // 客户端到服务器的数据，转发给目标服务器
                if (connectionInfo.TargetStream != null)
                {
                    await connectionInfo.TargetStream.WriteAsync(data, 0, data.Length);
                    await connectionInfo.TargetStream.FlushAsync();
                    
                    ProxyDataTransferred?.Invoke(this, (connectionId, data.Length));
                    
                    // 更新映射中的活动时间和字节数
                    _connectionMapping.UpdateActivity(connectionId);
                    _connectionMapping.UpdateBytesTransferred(connectionId, data.Length);
                }
            }
            else
            {
                // 服务器到客户端的数据，转发给客户端（通过P2P连接）
                var dataPacket = new ProxyDataPacket(connectionId, data, false);
                
                // 通过映射找到对应的用户ID
                var userId = _connectionMapping.GetUserId(connectionId);
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        ConsoleWriter.WriteLine($"[代理] 准备转发数据到客户端 {userId}: 连接ID={connectionId}, 数据大小={data.Length}字节");
                        await _client.SendPeerPacketAsync(userId, dataPacket);
                        ConsoleWriter.WriteLine($"[代理] 数据已转发到客户端 {userId}");
                        
                        // 更新映射中的活动时间和字节数
                        _connectionMapping.UpdateActivity(connectionId);
                        _connectionMapping.UpdateBytesTransferred(connectionId, data.Length);
                    }
                    catch (Exception ex)
                    {
                        ConsoleWriter.WriteLine($"[代理] 发送数据到 {userId} 失败: {ex.Message}");
                    }
                }
                else
                {
                    ConsoleWriter.WriteLine($"[代理] 未找到连接 {connectionId} 对应的用户ID");
                }
                
                ProxyDataTransferred?.Invoke(this, (connectionId, data.Length));
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[代理] 处理代理数据时出错: {connectionId}, 错误: {ex.Message}");
            await CloseProxyConnectionAsync(connectionId, "数据传输错误");
        }
    }

    /// <summary>
    /// 关闭代理连接
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="reason">关闭原因</param>
    public async Task CloseProxyConnectionAsync(string connectionId, string reason = "")
    {
        if (!_connections.TryGetValue(connectionId, out var connectionInfo))
        {
            return;
        }
        
        ConsoleWriter.WriteLine($"[代理] 关闭代理连接: {connectionId}, 原因: {reason}");
        
        // 先从字典中移除连接，防止重复关闭
        _connections.Remove(connectionId);
        _connectionMapping.RemoveMapping(connectionId);
        ConsoleWriter.WriteLine($"[代理] 连接已从字典中移除: {connectionId}, 剩余连接数: {_connections.Count}");
        
        try
        {
            // 关闭网络连接
            connectionInfo.TargetStream?.Close();
            connectionInfo.TargetClient?.Close();
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[代理] 关闭代理连接时出错: {connectionId}, 错误: {ex.Message}");
        }
        finally
        {
            ProxyConnectionClosed?.Invoke(this, connectionId);
        }
    }

    /// <summary>
    /// 从目标服务器转发数据到客户端
    /// </summary>
    /// <param name="connectionInfo">连接信息</param>
    private async Task ForwardDataFromTargetAsync(ProxyConnectionInfo connectionInfo)
    {
        try
        {
            var buffer = new byte[65536]; // 增加到64KB以支持更大的数据包
            
            while (connectionInfo.IsConnected && !_disposed)
            {
                if (connectionInfo.TargetStream == null) break;
                
                var bytesRead = await connectionInfo.TargetStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break; // 连接已关闭
                }
                
                // 转发数据到客户端 - 只使用实际读取的字节数
                var data = new byte[bytesRead];
                Array.Copy(buffer, 0, data, 0, bytesRead);
                
                ConsoleWriter.WriteLine($"[代理] 从目标服务器读取数据: 连接ID={connectionInfo.ConnectionId}, 大小={bytesRead}字节");
                
                var dataPacket = new ProxyDataPacket(connectionInfo.ConnectionId, data, false);
                
                // 通过映射找到对应的用户ID
                var userId = _connectionMapping.GetUserId(connectionInfo.ConnectionId);
                if (!string.IsNullOrEmpty(userId))
                {
                    // 再次检查连接是否仍然存在和有效
                    if (!_connections.ContainsKey(connectionInfo.ConnectionId) || !connectionInfo.IsConnected)
                    {
                        ConsoleWriter.WriteLine($"[代理] 连接已断开或不存在，跳过数据转发: {connectionInfo.ConnectionId}");
                        break;
                    }
                    
                    ConsoleWriter.WriteLine($"[代理] 准备转发目标服务器数据到客户端 {userId}: 连接ID={connectionInfo.ConnectionId}, 数据大小={bytesRead}字节");
                    await _client.SendPeerPacketAsync(userId, dataPacket);
                    ConsoleWriter.WriteLine($"[代理] 目标服务器数据已转发到客户端 {userId}, 时间戳: {DateTime.Now:HH:mm:ss.fff}");
                    
                    // 更新映射中的活动时间和字节数
                    _connectionMapping.UpdateActivity(connectionInfo.ConnectionId);
                    _connectionMapping.UpdateBytesTransferred(connectionInfo.ConnectionId, bytesRead);
                }
                else
                {
                    ConsoleWriter.WriteLine($"[代理] 未找到连接 {connectionInfo.ConnectionId} 对应的用户ID");
                }
                
                // 添加连接状态调试信息
                ConsoleWriter.WriteLine($"[代理] 当前活跃连接数: {_connections.Count}");
                
                connectionInfo.LastActivity = DateTime.UtcNow;
                ConsoleWriter.WriteLine($"[代理] 从目标服务器转发数据: {connectionInfo.ConnectionId}, 大小: {bytesRead} 字节");
                ProxyDataTransferred?.Invoke(this, (connectionInfo.ConnectionId, bytesRead));
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[代理] 从目标服务器转发数据时出错: {connectionInfo.ConnectionId}, 错误: {ex.Message}");
        }
        finally
        {
            ConsoleWriter.WriteLine($"[代理] 目标服务器连接已断开，准备关闭代理连接: {connectionInfo.ConnectionId}");
            await CloseProxyConnectionAsync(connectionInfo.ConnectionId, "目标服务器连接断开");
        }
    }

    /// <summary>
    /// 清理过期的连接
    /// </summary>
    private void CleanupExpiredConnections(object? state)
    {
        if (_disposed) return;
        
        var expiredConnections = new List<string>();
        var now = DateTime.UtcNow;
        
        foreach (var kvp in _connections)
        {
            var connectionId = kvp.Key;
            var connectionInfo = kvp.Value;
            
            // 检查连接超时
            if ((now - connectionInfo.LastActivity).TotalSeconds > ConnectionTimeout)
            {
                expiredConnections.Add(connectionId);
            }
        }
        
        foreach (var connectionId in expiredConnections)
        {
            _ = CloseProxyConnectionAsync(connectionId, "连接超时");
        }
        
        // 同时清理映射中的过期连接
        _connectionMapping.CleanupExpiredMappings(TimeSpan.FromSeconds(ConnectionTimeout));
        
        if (expiredConnections.Count > 0)
        {
            ConsoleWriter.WriteLine($"[代理] 清理了 {expiredConnections.Count} 个过期连接");
        }
    }

    /// <summary>
    /// 获取代理连接统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public ProxyManagerStats GetProxyStatistics()
    {
        var connectionStats = new ProxyConnectionStats
        {
            TotalConnections = _connections.Count,
            ActiveConnections = _connections.Values.Count(c => c.IsConnected),
            MaxConnections = MaxConnections,
            ConnectionTimeout = ConnectionTimeout
        };
        
        var proxyMappingStats = _connectionMapping.GetMappingStatistics();
        
        return new ProxyManagerStats
        {
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
    public List<ProxyConnectionInfo> GetAllConnections()
    {
        return _connections.Values.ToList();
    }

    /// <summary>
    /// 获取所有连接映射信息
    /// </summary>
    /// <returns>映射信息列表</returns>
    public List<ProxyConnectionMappingInfo> GetAllMappings()
    {
        return _connectionMapping.GetAllMappings();
    }

    /// <summary>
    /// 获取活跃连接映射信息
    /// </summary>
    /// <returns>活跃映射信息列表</returns>
    public List<ProxyConnectionMappingInfo> GetActiveMappings()
    {
        return _connectionMapping.GetActiveMappings();
    }

    /// <summary>
    /// 获取用户的所有连接映射信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>映射信息列表</returns>
    public List<ProxyConnectionMappingInfo> GetUserMappings(string userId)
    {
        return _connectionMapping.GetUserMappings(userId);
    }

    /// <summary>
    /// 根据连接ID获取用户ID
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <returns>用户ID</returns>
    public string? GetUserId(string connectionId)
    {
        return _connectionMapping.GetUserId(connectionId);
    }

    /// <summary>
    /// 根据用户ID获取连接ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>连接ID列表</returns>
    public List<string> GetConnectionIds(string userId)
    {
        return _connectionMapping.GetConnectionIds(userId);
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        ConsoleWriter.WriteLine("[代理] 正在关闭代理管理器...");
        
        _cleanupTimer?.Dispose();
        
        // 关闭所有连接
        var connectionIds = _connections.Keys.ToList();
        ConsoleWriter.WriteLine($"[代理] 正在关闭 {connectionIds.Count} 个活跃连接...");
        
        foreach (var connectionId in connectionIds)
        {
            _ = CloseProxyConnectionAsync(connectionId, "代理管理器关闭");
        }
        
        // 等待一小段时间让连接关闭完成
        Thread.Sleep(100);
        
        // 清理映射
        _connectionMapping?.Dispose();
        
        ConsoleWriter.WriteLine("[代理] 代理管理器已关闭");
    }
} 