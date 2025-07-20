using ConsoleInteractive;
using System.Net;
using System.Net.Sockets;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.PeerBound;

namespace Wrap.Remastered.Client;

/// <summary>
/// 本地代理连接信息
/// </summary>
public class LocalProxyConnectionInfo
{
    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 本地TCP客户端
    /// </summary>
    public TcpClient? LocalClient { get; set; }
    
    /// <summary>
    /// 本地网络流
    /// </summary>
    public NetworkStream? LocalStream { get; set; }
    
    /// <summary>
    /// 连接状态
    /// </summary>
    public bool IsConnected => LocalClient?.Connected == true;
    
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
/// 本地代理服务器
/// </summary>
public class LocalProxyServer : IDisposable
{
    private readonly IWrapClient _client;
    private readonly TcpListener? _listener;
    private readonly Dictionary<string, LocalProxyConnectionInfo> _connections = new();
    private readonly ProxyConnectionMapping _connectionMapping = new();
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

    public LocalProxyServer(IWrapClient client, int localPort, string targetAddress, int targetPort)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        LocalPort = localPort;
        TargetAddress = targetAddress;
        TargetPort = targetPort;
        
        try
        {
            _listener = new TcpListener(IPAddress.Any, LocalPort);
            ConsoleWriter.WriteLine($"[本地代理] 本地代理服务器初始化完成，监听端口: {LocalPort}");
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[本地代理] 初始化失败: {ex.Message}");
            throw;
        }
        
        // 启动清理定时器（每30秒检查一次）
        _cleanupTimer = new Timer(CleanupExpiredConnections, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// 启动本地代理服务器
    /// </summary>
    public async Task StartAsync()
    {
        if (_disposed || _listener == null)
        {
            throw new InvalidOperationException("代理服务器已关闭或未初始化");
        }
        
        try
        {
            _listener.Start();
            _isRunning = true;
            
            ConsoleWriter.WriteLine($"[本地代理] 本地代理服务器已启动，监听端口: {LocalPort}");
            ConsoleWriter.WriteLine($"[本地代理] 转发目标: {TargetAddress}:{TargetPort}");
            
            // 启动监听任务
            _ = Task.Run(ListenForConnectionsAsync);
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[本地代理] 启动失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 停止本地代理服务器
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning) return;
        
        _isRunning = false;
        
        try
        {
            _listener?.Stop();
            ConsoleWriter.WriteLine("[本地代理] 本地代理服务器已停止");
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[本地代理] 停止时出错: {ex.Message}");
        }
        
        // 关闭所有连接
        var connectionIds = _connections.Keys.ToList();
        foreach (var connectionId in connectionIds)
        {
            await CloseLocalConnectionAsync(connectionId, "服务器停止");
        }
    }

    /// <summary>
    /// 监听本地连接
    /// </summary>
    private async Task ListenForConnectionsAsync()
    {
        while (_isRunning && !_disposed)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleLocalConnectionAsync(client));
            }
            catch (Exception ex)
            {
                if (_isRunning && !_disposed)
                {
                    ConsoleWriter.WriteLine($"[本地代理] 接受连接时出错: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 处理本地连接
    /// </summary>
    /// <param name="client">本地TCP客户端</param>
    private async Task HandleLocalConnectionAsync(TcpClient client)
    {
        var connectionId = Guid.NewGuid().ToString();
        
        try
        {
            // 检查连接数量限制
            if (_connections.Count >= MaxConnections)
            {
                ConsoleWriter.WriteLine($"[本地代理] 连接数量已达上限 ({MaxConnections})，拒绝新连接");
                client.Close();
                return;
            }
            
            ConsoleWriter.WriteLine($"[本地代理] 接受本地连接: {connectionId}");
            
            var connectionInfo = new LocalProxyConnectionInfo
            {
                ConnectionId = connectionId,
                LocalClient = client,
                LocalStream = client.GetStream()
            };
            
            _connections[connectionId] = connectionInfo;
            
            // 添加连接映射（这里暂时使用连接ID作为用户ID，实际应该从房主信息获取）
            // 在实际应用中，需要从房间信息中获取房主ID
            var ownerUserId = GetOwnerUserId();
            if (!string.IsNullOrEmpty(ownerUserId))
            {
                ConsoleWriter.WriteLine($"[本地代理] 添加连接映射: {connectionId} -> {ownerUserId}");
                _connectionMapping.AddMapping(connectionId, ownerUserId);
            }
            else
            {
                ConsoleWriter.WriteLine($"[本地代理] 警告: 无法获取房主用户ID");
            }
            
            // 向房主发送代理连接请求
            var connectPacket = new ProxyConnectPacket(connectionId);
            await _client.SendPeerPacketAsync(ownerUserId ?? connectionId, connectPacket);
            
            ConsoleWriter.WriteLine($"[本地代理] 已发送代理连接请求: {connectionId} -> {TargetAddress}:{TargetPort}");
            
            // 启动数据转发任务
            _ = Task.Run(() => ForwardDataFromLocalAsync(connectionInfo));
            
            LocalConnectionEstablished?.Invoke(this, connectionId);
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[本地代理] 处理本地连接时出错: {connectionId}, 错误: {ex.Message}");
            client.Close();
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
            ConsoleWriter.WriteLine($"[本地代理] 连接不存在: {connectionId}");
            return;
        }
        
        if (!connectionInfo.IsConnected)
        {
            ConsoleWriter.WriteLine($"[本地代理] 连接已断开: {connectionId}");
            return;
        }
        
        try
        {
            connectionInfo.LastActivity = DateTime.UtcNow;
            
            if (!isClientToServer)
            {
                // 服务器到客户端的数据，转发给本地客户端
                if (connectionInfo.LocalStream != null)
                {
                    await connectionInfo.LocalStream.WriteAsync(data, 0, data.Length);
                    await connectionInfo.LocalStream.FlushAsync();
                    
                    ConsoleWriter.WriteLine($"[本地代理] 转发数据到本地客户端: {connectionId}, 大小: {data.Length} 字节");
                    LocalDataTransferred?.Invoke(this, (connectionId, data.Length));
                    
                    // 更新映射中的活动时间和字节数
                    _connectionMapping.UpdateActivity(connectionId);
                    _connectionMapping.UpdateBytesTransferred(connectionId, data.Length);
                }
            }
            else
            {
                // 客户端到服务器的数据，这里不应该收到，因为本地客户端的数据是通过ForwardDataFromLocalAsync处理的
                ConsoleWriter.WriteLine($"[本地代理] 收到意外的客户端到服务器数据: {connectionId}, 大小: {data.Length} 字节");
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[本地代理] 处理代理数据时出错: {connectionId}, 错误: {ex.Message}");
            await CloseLocalConnectionAsync(connectionId, "数据传输错误");
        }
    }

    /// <summary>
    /// 处理代理连接响应
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="success">是否成功</param>
    /// <param name="errorMessage">错误信息</param>
    public async Task HandleProxyResponseAsync(string connectionId, bool success, string errorMessage)
    {
        if (!_connections.TryGetValue(connectionId, out var connectionInfo))
        {
            return;
        }
        
        if (success)
        {
            ConsoleWriter.WriteLine($"[本地代理] 代理连接建立成功: {connectionId}");
            // 设置连接状态为活跃
            _connectionMapping.SetConnectionStatus(connectionId, true);
        }
        else
        {
            ConsoleWriter.WriteLine($"[本地代理] 代理连接建立失败: {connectionId}, 错误: {errorMessage}");
            // 设置连接状态为非活跃
            _connectionMapping.SetConnectionStatus(connectionId, false);
            await CloseLocalConnectionAsync(connectionId, "连接建立失败");
        }
    }

    /// <summary>
    /// 从本地客户端转发数据到房主
    /// </summary>
    /// <param name="connectionInfo">连接信息</param>
    private async Task ForwardDataFromLocalAsync(LocalProxyConnectionInfo connectionInfo)
    {
        try
        {
            var buffer = new byte[65536]; // 增加到64KB以支持更大的数据包
            
            while (connectionInfo.IsConnected && !_disposed)
            {
                if (connectionInfo.LocalStream == null) break;
                
                var bytesRead = await connectionInfo.LocalStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break; // 连接已关闭
                }
                
                // 转发数据到房主 - 只使用实际读取的字节数
                var data = new byte[bytesRead];
                Array.Copy(buffer, 0, data, 0, bytesRead);
                
                var dataPacket = new ProxyDataPacket(connectionInfo.ConnectionId, data, true);
                
                // 通过映射找到房主用户ID
                var ownerUserId = _connectionMapping.GetUserId(connectionInfo.ConnectionId);
                if (!string.IsNullOrEmpty(ownerUserId))
                {
                    ConsoleWriter.WriteLine($"[本地代理] 准备发送数据包到房主 {ownerUserId}: 连接ID={connectionInfo.ConnectionId}, 数据大小={bytesRead}字节");
                    await _client.SendPeerPacketAsync(ownerUserId, dataPacket);
                    ConsoleWriter.WriteLine($"[本地代理] 数据包已发送到房主 {ownerUserId}");
                    
                    // 更新映射中的活动时间和字节数
                    _connectionMapping.UpdateActivity(connectionInfo.ConnectionId);
                    _connectionMapping.UpdateBytesTransferred(connectionInfo.ConnectionId, bytesRead);
                }
                else
                {
                    ConsoleWriter.WriteLine($"[本地代理] 未找到连接 {connectionInfo.ConnectionId} 对应的房主用户ID");
                }
                
                connectionInfo.LastActivity = DateTime.UtcNow;
                ConsoleWriter.WriteLine($"[本地代理] 从本地客户端转发数据: {connectionInfo.ConnectionId}, 大小: {bytesRead} 字节");
                LocalDataTransferred?.Invoke(this, (connectionInfo.ConnectionId, bytesRead));
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[本地代理] 从本地客户端转发数据时出错: {connectionInfo.ConnectionId}, 错误: {ex.Message}");
        }
        finally
        {
            await CloseLocalConnectionAsync(connectionInfo.ConnectionId, "本地客户端连接断开");
        }
    }

    /// <summary>
    /// 关闭本地连接
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="reason">关闭原因</param>
    public async Task CloseLocalConnectionAsync(string connectionId, string reason = "")
    {
        if (!_connections.TryGetValue(connectionId, out var connectionInfo))
        {
            return;
        }
        
        ConsoleWriter.WriteLine($"[本地代理] 关闭本地连接: {connectionId}, 原因: {reason}");
        
        // 先从字典中移除连接，防止重复关闭
        _connections.Remove(connectionId);
        _connectionMapping.RemoveMapping(connectionId);
        ConsoleWriter.WriteLine($"[本地代理] 连接已从字典中移除: {connectionId}, 剩余连接数: {_connections.Count}");
        
        try
        {
            // 关闭网络连接
            connectionInfo.LocalStream?.Close();
            connectionInfo.LocalClient?.Close();
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[本地代理] 关闭本地连接时出错: {connectionId}, 错误: {ex.Message}");
        }
        finally
        {
            LocalConnectionClosed?.Invoke(this, connectionId);
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
            _ = CloseLocalConnectionAsync(connectionId, "连接超时");
        }
        
        // 同时清理映射中的过期连接
        _connectionMapping.CleanupExpiredMappings(TimeSpan.FromSeconds(ConnectionTimeout));
        
        if (expiredConnections.Count > 0)
        {
            ConsoleWriter.WriteLine($"[本地代理] 清理了 {expiredConnections.Count} 个过期连接");
        }
    }

    /// <summary>
    /// 获取本地代理统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public LocalProxyStats GetLocalProxyStatistics()
    {
        var connectionStats = new ProxyConnectionStats
        {
            TotalConnections = _connections.Count,
            ActiveConnections = _connections.Values.Count(c => c.IsConnected),
            MaxConnections = MaxConnections,
            ConnectionTimeout = ConnectionTimeout
        };
        
        var proxyMappingStats = _connectionMapping.GetMappingStatistics();
        
        return new LocalProxyStats
        {
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
    public List<LocalProxyConnectionInfo> GetAllConnections()
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

    /// <summary>
    /// 获取房主用户ID
    /// </summary>
    /// <returns>房主用户ID</returns>
    private string? GetOwnerUserId()
    {
        if (_client.CurrentRoomInfo != null)
        {
            return _client.CurrentRoomInfo.Owner.UserId;
        }
        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        ConsoleWriter.WriteLine("[本地代理] 正在关闭本地代理服务器...");
        
        _cleanupTimer?.Dispose();
        
        // 关闭所有连接
        var connectionIds = _connections.Keys.ToList();
        ConsoleWriter.WriteLine($"[本地代理] 正在关闭 {connectionIds.Count} 个活跃连接...");
        
        foreach (var connectionId in connectionIds)
        {
            _ = CloseLocalConnectionAsync(connectionId, "本地代理服务器关闭");
        }
        
        // 停止服务器
        _ = StopAsync();
        
        // 等待一小段时间让连接关闭完成
        Thread.Sleep(100);
        
        // 清理映射
        _connectionMapping?.Dispose();
        
        ConsoleWriter.WriteLine("[本地代理] 本地代理服务器已关闭");
    }
} 