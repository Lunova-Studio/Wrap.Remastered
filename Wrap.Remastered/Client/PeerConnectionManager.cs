using ConsoleInteractive;
using DotNetty.Common;
using System.Net;
using System.Net.Sockets;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.PeerBound;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Wrap.Remastered.Client;

/// <summary>
/// P2P连接信息
/// </summary>
public class PeerConnectionInfo
{
    /// <summary>
    /// 目标用户ID
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// TCP客户端连接
    /// </summary>
    public TcpClient? TcpClient { get; set; }
    
    /// <summary>
    /// 网络流
    /// </summary>
    public NetworkStream? NetworkStream { get; set; }
    
    /// <summary>
    /// 连接状态
    /// </summary>
    public bool IsConnected => TcpClient?.Connected == true;
    
    /// <summary>
    /// 最后心跳时间
    /// </summary>
    public DateTime LastKeepAlive { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 期望的心跳响应值
    /// </summary>
    public int? ExpectedKeepAliveResponse { get; set; }
    
    /// <summary>
    /// 最后发送心跳的时间
    /// </summary>
    public DateTime LastSentKeepAlive { get; set; } = DateTime.UtcNow;

    public SemaphoreSlim WriteLock { get; } = new(1, 1);
}

/// <summary>
/// P2P连接管理器
/// </summary>
public class PeerConnectionManager : IDisposable
{
    private readonly Dictionary<string, PeerConnectionInfo> _connections = new();
    private readonly IWrapClient _client;
    private Timer _keepAliveTimer;
    private Timer _keepAliveTimeoutTimer;
    private bool _disposed = false;
    private readonly Dictionary<string, long> _lastSequenceId = new();
    private readonly Dictionary<string, SortedDictionary<long, ProxyDataPacket>> _pendingPackets = new();

    // P2P连接状态事件
    public event EventHandler<string>? PeerConnected;
    public event EventHandler<string>? PeerDisconnected;
    public event EventHandler<(string targetUserId, PeerKeepAlivePacket packet)>? PeerKeepAliveReceived;

    public PeerConnectionManager(IWrapClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        
        // 获取P2P心跳间隔配置，默认为10秒（与服务端保持一致）
        var heartbeatInterval = client is WrapClient wrapClient && wrapClient.Profile != null 
            ? wrapClient.Profile.PeerHeartbeatInterval 
            : 10;
        
        // 验证心跳间隔在合理范围内（5-60秒）
        if (heartbeatInterval < 5 || heartbeatInterval > 60)
        {
            ConsoleWriter.WriteLine($"[P2P] 警告: P2P心跳间隔 {heartbeatInterval}秒 超出合理范围(5-60秒)，使用默认值10秒");
            heartbeatInterval = 10;
        }
        
        // 启动心跳定时器
        _keepAliveTimer = new Timer(async (c) => await SendKeepAliveToAllAsync(c), null, TimeSpan.FromSeconds(heartbeatInterval), TimeSpan.FromSeconds(heartbeatInterval));
        
        // 启动心跳超时检查定时器（每5秒检查一次）
        _keepAliveTimeoutTimer = new Timer(CheckKeepAliveTimeouts, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        
        ConsoleWriter.WriteLine($"[P2P] P2P心跳间隔设置为: {heartbeatInterval}秒");
    }

    /// <summary>
    /// 添加P2P连接
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="tcpClient">TCP客户端</param>
    public void AddConnection(string targetUserId, TcpClient tcpClient)
    {
        if (string.IsNullOrEmpty(targetUserId)) throw new ArgumentNullException(nameof(targetUserId));
        if (tcpClient == null) throw new ArgumentNullException(nameof(tcpClient));

        var connectionInfo = new PeerConnectionInfo
        {
            TargetUserId = targetUserId,
            TcpClient = tcpClient,
            NetworkStream = tcpClient.GetStream(),
            LastKeepAlive = DateTime.UtcNow
        };

        _connections[targetUserId] = connectionInfo;
        
        // 启动数据接收任务
        _ = Task.Run(() => ReceiveDataAsync(connectionInfo));
        
        ConsoleWriter.WriteLine($"[P2P] 添加与 {targetUserId} 的P2P连接");
        
        // 触发连接事件
        PeerConnected?.Invoke(_client, targetUserId);
    }

    /// <summary>
    /// 关闭P2P连接
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    public void CloseConnection(string targetUserId)
    {
        if (string.IsNullOrEmpty(targetUserId))
        {
            ConsoleWriter.WriteLine("[P2P] 警告: 尝试关闭空用户ID的连接");
            return;
        }

        if (_connections.TryGetValue(targetUserId, out var connectionInfo))
        {
            ConsoleWriter.WriteLine($"[P2P] 正在关闭与 {targetUserId} 的P2P连接...");
            
            try
            {
                // 关闭网络流
                if (connectionInfo.NetworkStream != null)
                {
                    connectionInfo.NetworkStream.Close();
                    connectionInfo.NetworkStream.Dispose();
                }
                
                // 关闭TCP客户端
                if (connectionInfo.TcpClient != null)
                {
                    connectionInfo.TcpClient.Close();
                    connectionInfo.TcpClient.Dispose();
                }
                
                // 从连接列表中移除
                _connections.Remove(targetUserId);
                
                ConsoleWriter.WriteLine($"[P2P] 成功关闭与 {targetUserId} 的P2P连接");
                
                // 触发断开连接事件
                PeerDisconnected?.Invoke(_client, targetUserId);
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteLine($"[P2P] 关闭与 {targetUserId} 的连接时出错: {ex.Message}");
                
                // 即使出错也要从连接列表中移除
                _connections.Remove(targetUserId);
            }
        }
        else
        {
            ConsoleWriter.WriteLine($"[P2P] 警告: 尝试关闭不存在的连接: {targetUserId}");
        }
    }

    /// <summary>
    /// 关闭所有P2P连接
    /// </summary>
    public void CloseAllConnections()
    {
        ConsoleWriter.WriteLine($"[P2P] 正在关闭所有P2P连接，共 {_connections.Count} 个连接...");
        
        var connectionIds = _connections.Keys.ToList();
        
        foreach (var targetUserId in connectionIds)
        {
            CloseConnection(targetUserId);
        }
        
        ConsoleWriter.WriteLine("[P2P] 所有P2P连接已关闭");
    }

    /// <summary>
    /// 检查连接状态
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <returns>连接状态信息</returns>
    public ConnectionStatus GetConnectionStatus(string targetUserId)
    {
        if (string.IsNullOrEmpty(targetUserId))
        {
            return new ConnectionStatus { IsConnected = false, Error = "用户ID为空" };
        }

        if (!_connections.TryGetValue(targetUserId, out var connectionInfo))
        {
            return new ConnectionStatus { IsConnected = false, Error = "连接不存在" };
        }

        return new ConnectionStatus
        {
            IsConnected = connectionInfo.IsConnected,
            TargetUserId = targetUserId,
            LastKeepAlive = connectionInfo.LastKeepAlive,
            LastSentKeepAlive = connectionInfo.LastSentKeepAlive,
            HasPendingKeepAlive = connectionInfo.ExpectedKeepAliveResponse.HasValue,
            ExpectedResponse = connectionInfo.ExpectedKeepAliveResponse
        };
    }

    /// <summary>
    /// 移除P2P连接（内部使用）
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    private void RemoveConnection(string targetUserId)
    {
        if (_connections.TryGetValue(targetUserId, out var connectionInfo))
        {
            connectionInfo.NetworkStream?.Close();
            connectionInfo.TcpClient?.Close();
            _connections.Remove(targetUserId);
            
            ConsoleWriter.WriteLine($"[P2P] 移除与 {targetUserId} 的P2P连接");
            
            // 触发断开连接事件
            PeerDisconnected?.Invoke(_client, targetUserId);
        }
    }

    /// <summary>
    /// 获取连接信息
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <returns>连接信息</returns>
    public PeerConnectionInfo? GetConnection(string targetUserId)
    {
        return _connections.TryGetValue(targetUserId, out var connectionInfo) ? connectionInfo : null;
    }

    /// <summary>
    /// 检查是否与指定用户有P2P连接
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <returns>是否有连接</returns>
    public bool HasConnection(string targetUserId)
    {
        return _connections.ContainsKey(targetUserId) && _connections[targetUserId].IsConnected;
    }

    /// <summary>
    /// 发送P2P数据包
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="packet">数据包</param>
    public async Task SendPacketAsync(string targetUserId, IPeerBoundPacket packet)
    {
        ConsoleWriter.WriteLine($"[P2P] SendPacketAsync packetType: {packet.GetPacketType()} to {targetUserId}");
        if (string.IsNullOrEmpty(targetUserId)) throw new ArgumentNullException(nameof(targetUserId));
        if (packet == null) throw new ArgumentNullException(nameof(packet));

        if (!_connections.TryGetValue(targetUserId, out var connectionInfo))
        {
            throw new InvalidOperationException($"与用户 {targetUserId} 的P2P连接不存在");
        }

        if (!connectionInfo.IsConnected)
        {
            throw new InvalidOperationException($"与用户 {targetUserId} 的P2P连接已断开");
        }

        try
        {
            var serializer = packet.GetSerializer();
            var data = serializer.Serialize(packet);
            packet.OnSerialize(ref data);

            // 创建包含数据包类型和数据的完整数据包
            var packetData = new byte[4 + data.Length];
            BitConverter.GetBytes((int)packet.GetPacketType()).CopyTo(packetData, 0);
            data.CopyTo(packetData, 4);

            if (connectionInfo.NetworkStream != null)
            {
                await connectionInfo.WriteLock.WaitAsync();
                try
                {
                    ConsoleWriter.WriteLine($"[P2P] Write to {targetUserId} type={packet.GetPacketType()} size={packetData.Length}");
                    connectionInfo.NetworkStream.WriteInt(packetData.Length);
                    await connectionInfo.NetworkStream.WriteAsync(packetData, 0, packetData.Length);
                    await connectionInfo.NetworkStream.FlushAsync();
                    ConsoleWriter.WriteLine($"[P2P] Write to {targetUserId} finished type={packet.GetPacketType()} size={packetData.Length}");
                }
                finally
                {
                    connectionInfo.WriteLock.Release();
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[P2P] 向 {targetUserId} 发送数据包失败: {ex.Message}");
            RemoveConnection(targetUserId);
            throw;
        }
    }

    /// <summary>
    /// 发送P2P数据包（同步版本）
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="packet">数据包</param>
    public void SendPacket(string targetUserId, IPeerBoundPacket packet)
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        SendPacketAsync(targetUserId, packet).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    /// <summary>
    /// 发送心跳包
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    public async Task SendKeepAliveAsync(string targetUserId)
    {
        var keepAlivePacket = new PeerKeepAlivePacket();
        await SendPacketAsync(targetUserId, keepAlivePacket);
    }

    /// <summary>
    /// 向所有连接发送心跳
    /// </summary>
    private async Task SendKeepAliveToAllAsync(object? state)
    {
        if (_disposed) return;

        var disconnectedUsers = new List<string>();

        foreach (var kvp in _connections)
        {
            var targetUserId = kvp.Key;
            var connectionInfo = kvp.Value;

            try
            {
                if (connectionInfo.IsConnected)
                {
                    // 创建心跳包
                    var keepAlivePacket = new PeerKeepAlivePacket();
                    
                    // 设置期望的响应值
                    connectionInfo.ExpectedKeepAliveResponse = keepAlivePacket.Value;
                    connectionInfo.LastSentKeepAlive = DateTime.UtcNow;
                    
                    // 发送心跳包
                    await SendPacketAsync(targetUserId, keepAlivePacket);
                    
                    ConsoleWriter.WriteLine($"[P2P] 已向 {targetUserId} 发送心跳包，期望响应值: {keepAlivePacket.Value}");
                }
                else
                {
                    disconnectedUsers.Add(targetUserId);
                }
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteLine($"[P2P] 向 {targetUserId} 发送心跳失败: {ex.Message}");
                disconnectedUsers.Add(targetUserId);
            }
        }

        // 移除断开的连接
        foreach (var userId in disconnectedUsers)
        {
            RemoveConnection(userId);
        }
    }

    /// <summary>
    /// 接收P2P数据
    /// </summary>
    /// <param name="connectionInfo">连接信息</param>
    private async Task ReceiveDataAsync(PeerConnectionInfo connectionInfo)
    {
        try
        {
            var buffer = new byte[65536]; // 增加到64KB以支持更大的数据包
            
            while (connectionInfo.IsConnected)
            {
                if (connectionInfo.NetworkStream == null) break;

                var bytesRead = connectionInfo.NetworkStream.ReadInt32();
                var data = connectionInfo.NetworkStream.ReadBytes(bytesRead);
                
                await ProcessReceivedDataAsync(connectionInfo.TargetUserId, data);
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[P2P] 接收来自 {connectionInfo.TargetUserId} 的数据时出错: {ex.Message}");
        }
        finally
        {
            RemoveConnection(connectionInfo.TargetUserId);
        }
    }

    /// <summary>
    /// 处理接收到的P2P数据
    /// </summary>
    /// <param name="targetUserId">发送方用户ID</param>
    /// <param name="data">数据</param>
    private async Task ProcessReceivedDataAsync(string targetUserId, byte[] data)
    {
        try
        {
            if (data.Length < 4)
            {
                ConsoleWriter.WriteLine($"[P2P] 收到来自 {targetUserId} 的数据包过短");
                return;
            }

            // 解析数据包类型
            var packetType = BitConverter.ToInt32(data, 0);
            var packetData = new byte[data.Length - 4];
            Array.Copy(data, 4, packetData, 0, packetData.Length);


            ConsoleWriter.WriteLine($"[P2P] 收到来自 {targetUserId} 的数据包类型: {((PeerBoundPacketType)packetType).ToString()}");
            // 根据数据包类型处理
            switch ((PeerBoundPacketType)packetType)
            {
                case PeerBoundPacketType.PeerKeepAlivePacket:
                    await HandlePeerKeepAlivePacketAsync(targetUserId, packetData);
                    break;
                case PeerBoundPacketType.PeerKeepAliveResponsePacket:
                    await HandlePeerKeepAliveResponsePacketAsync(targetUserId, packetData);
                    break;
                case PeerBoundPacketType.ProxyConnectPacket:
                    await HandleProxyConnectPacketAsync(targetUserId, packetData);
                    break;
                case PeerBoundPacketType.ProxyDataPacket:
                    await HandleProxyDataPacketAsync(targetUserId, packetData);
                    break;
                case PeerBoundPacketType.ProxyDisconnectPacket:
                    await HandleProxyDisconnectPacketAsync(targetUserId, packetData);
                    break;
                case PeerBoundPacketType.ProxyResponsePacket:
                    await HandleProxyResponsePacketAsync(targetUserId, packetData);
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[P2P] 处理来自 {targetUserId} 的数据时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理P2P心跳包
    /// </summary>
    /// <param name="targetUserId">发送方用户ID</param>
    /// <param name="packetData">数据包数据</param>
    private async Task HandlePeerKeepAlivePacketAsync(string targetUserId, byte[] packetData)
    {
        try
        {
            var serializer = new PeerKeepAlivePacketSerializer();
            var packet = (PeerKeepAlivePacket)serializer.Deserialize(packetData);
            
            ConsoleWriter.WriteLine($"[P2P] 收到来自 {targetUserId} 的心跳包，值: {packet.Value}");
            
            // 触发心跳事件
            PeerKeepAliveReceived?.Invoke(_client, (targetUserId, packet));
            
            // 发送心跳响应包
            var responsePacket = new PeerKeepAliveResponsePacket(packet.Value);
            await SendPacketAsync(targetUserId, responsePacket);
            
            ConsoleWriter.WriteLine($"[P2P] 已向 {targetUserId} 发送心跳响应包，值: {packet.Value}");
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[P2P] 处理来自 {targetUserId} 的心跳包时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理P2P心跳响应包
    /// </summary>
    /// <param name="targetUserId">发送方用户ID</param>
    /// <param name="packetData">数据包数据</param>
    private async Task HandlePeerKeepAliveResponsePacketAsync(string targetUserId, byte[] packetData)
    {
        try
        {
            var serializer = new PeerKeepAliveResponsePacketSerializer();
            var packet = (PeerKeepAliveResponsePacket)serializer.Deserialize(packetData);
            
            ConsoleWriter.WriteLine($"[P2P] 收到来自 {targetUserId} 的心跳响应包，值: {packet.Value}");
            
            // 验证响应值
            var connectionInfo = GetConnection(targetUserId);
            if (connectionInfo != null && connectionInfo.ExpectedKeepAliveResponse.HasValue)
            {
                if (packet.Value == connectionInfo.ExpectedKeepAliveResponse.Value)
                {
                    // 验证成功，更新最后心跳时间
                    connectionInfo.LastKeepAlive = DateTime.UtcNow;
                    connectionInfo.ExpectedKeepAliveResponse = null; // 清除期望值
                    ConsoleWriter.WriteLine($"[P2P] 心跳验证成功: {targetUserId}");
                }
                else
                {
                    // 验证失败，断开连接
                    ConsoleWriter.WriteLine($"[P2P] 心跳验证失败: {targetUserId}，期望值: {connectionInfo.ExpectedKeepAliveResponse.Value}，实际值: {packet.Value}");
                    CloseConnection(targetUserId);
                }
            }
            else
            {
                ConsoleWriter.WriteLine($"[P2P] 收到来自 {targetUserId} 的意外心跳响应包");
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[P2P] 处理来自 {targetUserId} 的心跳响应包时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理代理连接请求包
    /// </summary>
    /// <param name="targetUserId">发送方用户ID</param>
    /// <param name="packetData">数据包数据</param>
    private async Task HandleProxyConnectPacketAsync(string targetUserId, byte[] packetData)
    {
        try
        {
            var serializer = new ProxyConnectPacketSerializer();
            var packet = (ProxyConnectPacket)serializer.Deserialize(packetData);
            
            ConsoleWriter.WriteLine($"[P2P] 收到来自 {targetUserId} 的代理连接请求: {packet.ConnectionId}");
            
            // 转发给代理管理器处理
            if (_client is WrapClient wrapClient && wrapClient.ProxyManager != null)
            {
                var success = await wrapClient.ProxyManager.HandleProxyConnectRequestAsync(
                    packet.ConnectionId, targetUserId);
                
                // 发送响应包
                var responsePacket = new ProxyResponsePacket(packet.ConnectionId, success, 
                    success ? "" : "连接建立失败");
                await SendPacketAsync(targetUserId, responsePacket);
            }
            else
            {
                ConsoleWriter.WriteLine($"[P2P] 代理管理器不可用，拒绝代理连接请求: {packet.ConnectionId}");
                var responsePacket = new ProxyResponsePacket(packet.ConnectionId, false, "代理管理器不可用");
                await SendPacketAsync(targetUserId, responsePacket);
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[P2P] 处理代理连接请求时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理代理数据包
    /// </summary>
    /// <param name="targetUserId">发送方用户ID</param>
    /// <param name="packetData">数据包数据</param>
    private async Task HandleProxyDataPacketAsync(string targetUserId, byte[] packetData)
    {
        try
        {
            var serializer = new ProxyDataPacketSerializer();
            var packet = (ProxyDataPacket)serializer.Deserialize(packetData);
            // 顺序校验与缓存
            if (!_lastSequenceId.TryGetValue(packet.ConnectionId, out var lastSeq))
                lastSeq = 0;
            if (!_pendingPackets.TryGetValue(packet.ConnectionId, out var buffer))
            {
                buffer = new SortedDictionary<long, ProxyDataPacket>();
                _pendingPackets[packet.ConnectionId] = buffer;
            }
            if (packet.SequenceId == lastSeq + 1)
            {
                // 正常顺序，处理并递增
                await ProcessProxyDataPacketAsync(packet);
                _lastSequenceId[packet.ConnectionId] = packet.SequenceId;
                // 检查buffer中是否有后续包
                while (buffer.TryGetValue(_lastSequenceId[packet.ConnectionId] + 1, out var nextPacket))
                {
                    buffer.Remove(_lastSequenceId[packet.ConnectionId] + 1);
                    await ProcessProxyDataPacketAsync(nextPacket);
                    _lastSequenceId[packet.ConnectionId]++;
                }
            }
            else if (packet.SequenceId > lastSeq + 1)
            {
                // 乱序，缓存
                buffer[packet.SequenceId] = packet;
            }
            // 否则为重复包，直接丢弃
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[P2P] 处理代理数据包时出错: {ex.Message}");
        }
    }
    // 新增：实际处理ProxyDataPacket的方法
    private async Task ProcessProxyDataPacketAsync(ProxyDataPacket packet)
    {
        if (packet.IsClientToServer)
        {
            await _client.ProxyManager.HandleProxyDataAsync(
                packet.ConnectionId, packet);
        }
        else
        {
            ConsoleWriter.WriteLine($"[代理] ProcessProxyDataPacketAsync");
            _client.LocalProxyServer.HandleProxyDataSync(packet.ConnectionId, packet);
        }
    }

    /// <summary>
    /// 处理代理断开连接包
    /// </summary>
    /// <param name="targetUserId">发送方用户ID</param>
    /// <param name="packetData">数据包数据</param>
    private async Task HandleProxyDisconnectPacketAsync(string targetUserId, byte[] packetData)
    {
        try
        {
            var serializer = new ProxyDisconnectPacketSerializer();
            var packet = (ProxyDisconnectPacket)serializer.Deserialize(packetData);
            
            ConsoleWriter.WriteLine($"[P2P] 收到来自 {targetUserId} 的代理断开连接包: {packet.ConnectionId}, 原因: {packet.Reason}");
            
            // 转发给代理管理器处理
            if (_client is WrapClient wrapClient && wrapClient.ProxyManager != null)
            {
                await wrapClient.ProxyManager.CloseProxyConnectionAsync(packet.ConnectionId, packet.Reason);
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[P2P] 处理代理断开连接包时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理代理响应包
    /// </summary>
    /// <param name="targetUserId">发送方用户ID</param>
    /// <param name="packetData">数据包数据</param>
    private async Task HandleProxyResponsePacketAsync(string targetUserId, byte[] packetData)
    {
        try
        {
            var serializer = new ProxyResponsePacketSerializer();
            var packet = (ProxyResponsePacket)serializer.Deserialize(packetData);
            
            ConsoleWriter.WriteLine($"[P2P] 收到来自 {targetUserId} 的代理响应包: {packet.ConnectionId}, 成功: {packet.Success}");
            
            // 转发给本地代理服务器处理
            if (_client is WrapClient wrapClient && wrapClient.LocalProxyServer != null)
            {
                await wrapClient.LocalProxyServer.HandleProxyResponseAsync(
                    packet.ConnectionId, packet.Success, packet.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"[P2P] 处理代理响应包时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取所有活跃连接
    /// </summary>
    /// <returns>活跃连接列表</returns>
    public IEnumerable<string> GetActiveConnections()
    {
        return _connections.Where(kvp => kvp.Value.IsConnected).Select(kvp => kvp.Key);
    }

    /// <summary>
    /// 获取连接数量
    /// </summary>
    public int ConnectionCount => _connections.Count;

    /// <summary>
    /// 获取心跳状态信息
    /// </summary>
    /// <returns>心跳状态信息</returns>
    public Dictionary<string, object> GetKeepAliveStatus()
    {
        var status = new Dictionary<string, object>();
        
        foreach (var kvp in _connections)
        {
            var targetUserId = kvp.Key;
            var connectionInfo = kvp.Value;
            
            status[targetUserId] = new
            {
                IsConnected = connectionInfo.IsConnected,
                LastKeepAlive = connectionInfo.LastKeepAlive,
                LastSentKeepAlive = connectionInfo.LastSentKeepAlive,
                ExpectedResponse = connectionInfo.ExpectedKeepAliveResponse,
                HasPendingKeepAlive = connectionInfo.ExpectedKeepAliveResponse.HasValue
            };
        }
        
        return status;
    }

    /// <summary>
    /// 设置P2P心跳间隔
    /// </summary>
    /// <param name="intervalSeconds">心跳间隔（秒）</param>
    public void SetHeartbeatInterval(int intervalSeconds)
    {
        if (intervalSeconds < 5 || intervalSeconds > 60)
        {
            ConsoleWriter.WriteLine($"[P2P] 警告: 心跳间隔 {intervalSeconds}秒 超出合理范围(5-60秒)");
            return;
        }

        // 重新创建定时器
        _keepAliveTimer?.Dispose();
        _keepAliveTimer = new Timer(async (c) => SendKeepAliveToAllAsync(c), null, TimeSpan.FromSeconds(intervalSeconds), TimeSpan.FromSeconds(intervalSeconds));
        
        ConsoleWriter.WriteLine($"[P2P] P2P心跳间隔已调整为: {intervalSeconds}秒");
    }

    /// <summary>
    /// 检查心跳超时
    /// </summary>
    private void CheckKeepAliveTimeouts(object? state)
    {
        if (_disposed) return;

        var timeoutUsers = new List<string>();
        var now = DateTime.UtcNow;

        foreach (var kvp in _connections)
        {
            var targetUserId = kvp.Key;
            var connectionInfo = kvp.Value;

            // 检查是否有未响应的心跳（超过15秒未响应）
            if (connectionInfo.ExpectedKeepAliveResponse.HasValue && 
                (now - connectionInfo.LastSentKeepAlive).TotalSeconds > 15)
            {
                ConsoleWriter.WriteLine($"[P2P] 心跳超时: {targetUserId}，最后发送时间: {connectionInfo.LastSentKeepAlive}");
                timeoutUsers.Add(targetUserId);
            }
        }

        // 断开超时的连接
        foreach (var userId in timeoutUsers)
        {
            CloseConnection(userId);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        _keepAliveTimer?.Dispose();
        _keepAliveTimeoutTimer?.Dispose();

        foreach (var connectionInfo in _connections.Values)
        {
            connectionInfo.NetworkStream?.Close();
            connectionInfo.TcpClient?.Close();
        }

        _connections.Clear();
    }
} 