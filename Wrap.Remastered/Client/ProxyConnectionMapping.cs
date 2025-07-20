using ConsoleInteractive;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Wrap.Remastered.Client;

/// <summary>
/// 代理连接映射信息
/// </summary>
public class ProxyConnectionMappingInfo
{
    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 连接状态
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 目标地址
    /// </summary>
    public string TargetAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 目标端口
    /// </summary>
    public int TargetPort { get; set; }
    
    /// <summary>
    /// 传输的字节数
    /// </summary>
    public long BytesTransferred { get; set; }
    
    /// <summary>
    /// 连接持续时间
    /// </summary>
    public TimeSpan Duration => DateTime.UtcNow - CreatedTime;
    
    /// <summary>
    /// 空闲时间
    /// </summary>
    public TimeSpan IdleTime => DateTime.UtcNow - LastActivity;
}

/// <summary>
/// 代理连接映射管理器
/// </summary>
public class ProxyConnectionMapping : IDisposable
{
    private readonly Dictionary<string, ProxyConnectionMappingInfo> _connectionToUser = new();
    private readonly Dictionary<string, List<string>> _userToConnections = new();
    private readonly object _lock = new object();
    private bool _disposed = false;
    
    /// <summary>
    /// 添加连接映射
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="userId">用户ID</param>
    public void AddMapping(string connectionId, string userId)
    {
        if (string.IsNullOrEmpty(connectionId) || string.IsNullOrEmpty(userId))
            throw new ArgumentException("连接ID和用户ID不能为空");
        
        lock (_lock)
        {
            var mappingInfo = new ProxyConnectionMappingInfo
            {
                ConnectionId = connectionId,
                UserId = userId
            };
            
            _connectionToUser[connectionId] = mappingInfo;
            
            if (!_userToConnections.ContainsKey(userId))
            {
                _userToConnections[userId] = new List<string>();
            }
            _userToConnections[userId].Add(connectionId);
            
            ConsoleWriter.WriteLine($"[代理映射] 添加映射: {connectionId} -> {userId}");
        }
    }
    
    /// <summary>
    /// 移除连接映射
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    public void RemoveMapping(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId)) return;
        
        lock (_lock)
        {
            if (_connectionToUser.TryGetValue(connectionId, out var mappingInfo))
            {
                var userId = mappingInfo.UserId;
                
                _connectionToUser.Remove(connectionId);
                
                if (_userToConnections.ContainsKey(userId))
                {
                    _userToConnections[userId].Remove(connectionId);
                    if (_userToConnections[userId].Count == 0)
                    {
                        _userToConnections.Remove(userId);
                    }
                }
                
                ConsoleWriter.WriteLine($"[代理映射] 移除映射: {connectionId} -> {userId}");
            }
        }
    }
    
    /// <summary>
    /// 根据连接ID获取用户ID
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <returns>用户ID，如果不存在则返回null</returns>
    public string? GetUserId(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId)) return null;
        
        lock (_lock)
        {
            return _connectionToUser.TryGetValue(connectionId, out var mappingInfo) ? mappingInfo.UserId : null;
        }
    }
    
    /// <summary>
    /// 根据用户ID获取所有连接ID
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>连接ID列表</returns>
    public List<string> GetConnectionIds(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return new List<string>();
        
        lock (_lock)
        {
            return _userToConnections.TryGetValue(userId, out var connectionIds) 
                ? new List<string>(connectionIds) 
                : new List<string>();
        }
    }
    
    /// <summary>
    /// 获取连接映射信息
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <returns>映射信息，如果不存在则返回null</returns>
    public ProxyConnectionMappingInfo? GetMappingInfo(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId)) return null;
        
        lock (_lock)
        {
            return _connectionToUser.TryGetValue(connectionId, out var mappingInfo) ? mappingInfo : null;
        }
    }
    
    /// <summary>
    /// 更新连接活动时间
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    public void UpdateActivity(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId)) return;
        
        lock (_lock)
        {
            if (_connectionToUser.TryGetValue(connectionId, out var mappingInfo))
            {
                mappingInfo.LastActivity = DateTime.UtcNow;
            }
        }
    }
    
    /// <summary>
    /// 更新传输字节数
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="bytes">传输的字节数</param>
    public void UpdateBytesTransferred(string connectionId, long bytes)
    {
        if (string.IsNullOrEmpty(connectionId)) return;
        
        lock (_lock)
        {
            if (_connectionToUser.TryGetValue(connectionId, out var mappingInfo))
            {
                mappingInfo.BytesTransferred += bytes;
                mappingInfo.LastActivity = DateTime.UtcNow;
            }
        }
    }
    
    /// <summary>
    /// 设置连接状态
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="isActive">是否活跃</param>
    public void SetConnectionStatus(string connectionId, bool isActive)
    {
        if (string.IsNullOrEmpty(connectionId)) return;
        
        lock (_lock)
        {
            if (_connectionToUser.TryGetValue(connectionId, out var mappingInfo))
            {
                mappingInfo.IsActive = isActive;
            }
        }
    }
    
    /// <summary>
    /// 检查连接是否存在
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <returns>是否存在</returns>
    public bool HasConnection(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId)) return false;
        
        lock (_lock)
        {
            return _connectionToUser.ContainsKey(connectionId);
        }
    }
    
    /// <summary>
    /// 检查用户是否有连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否有连接</returns>
    public bool HasUserConnections(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        
        lock (_lock)
        {
            return _userToConnections.ContainsKey(userId) && _userToConnections[userId].Count > 0;
        }
    }
    
    /// <summary>
    /// 获取所有连接映射信息
    /// </summary>
    /// <returns>映射信息列表</returns>
    public List<ProxyConnectionMappingInfo> GetAllMappings()
    {
        lock (_lock)
        {
            return _connectionToUser.Values.ToList();
        }
    }
    
    /// <summary>
    /// 获取活跃连接映射信息
    /// </summary>
    /// <returns>活跃映射信息列表</returns>
    public List<ProxyConnectionMappingInfo> GetActiveMappings()
    {
        lock (_lock)
        {
            return _connectionToUser.Values.Where(m => m.IsActive).ToList();
        }
    }
    
    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public Dictionary<string, object> GetStatistics()
    {
        lock (_lock)
        {
            var totalConnections = _connectionToUser.Count;
            var activeConnections = _connectionToUser.Values.Count(m => m.IsActive);
            var totalUsers = _userToConnections.Count;
            var totalBytesTransferred = _connectionToUser.Values.Sum(m => m.BytesTransferred);
            
            return new Dictionary<string, object>
            {
                ["TotalConnections"] = totalConnections,
                ["ActiveConnections"] = activeConnections,
                ["TotalUsers"] = totalUsers,
                ["TotalBytesTransferred"] = totalBytesTransferred
            };
        }
    }

    /// <summary>
    /// 获取映射统计信息
    /// </summary>
    /// <returns>映射统计信息</returns>
    public ProxyMappingStats GetMappingStatistics()
    {
        lock (_lock)
        {
            var totalConnections = _connectionToUser.Count;
            var activeConnections = _connectionToUser.Values.Count(m => m.IsActive);
            var totalUsers = _userToConnections.Count;
            var totalBytesTransferred = _connectionToUser.Values.Sum(m => m.BytesTransferred);
            
            return new ProxyMappingStats
            {
                TotalConnections = totalConnections,
                ActiveConnections = activeConnections,
                TotalUsers = totalUsers,
                TotalBytesTransferred = totalBytesTransferred
            };
        }
    }
    
    /// <summary>
    /// 清理过期的连接映射
    /// </summary>
    /// <param name="maxIdleTime">最大空闲时间</param>
    /// <returns>清理的连接数量</returns>
    public int CleanupExpiredMappings(TimeSpan maxIdleTime)
    {
        var expiredConnections = new List<string>();
        var now = DateTime.UtcNow;
        
        lock (_lock)
        {
            foreach (var kvp in _connectionToUser)
            {
                var connectionId = kvp.Key;
                var mappingInfo = kvp.Value;
                
                if ((now - mappingInfo.LastActivity) > maxIdleTime)
                {
                    expiredConnections.Add(connectionId);
                }
            }
            
            foreach (var connectionId in expiredConnections)
            {
                RemoveMapping(connectionId);
            }
        }
        
        if (expiredConnections.Count > 0)
        {
            ConsoleWriter.WriteLine($"[代理映射] 清理了 {expiredConnections.Count} 个过期连接映射");
        }
        
        return expiredConnections.Count;
    }
    
    /// <summary>
    /// 获取用户的所有连接映射信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>映射信息列表</returns>
    public List<ProxyConnectionMappingInfo> GetUserMappings(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return new List<ProxyConnectionMappingInfo>();
        
        lock (_lock)
        {
            if (_userToConnections.TryGetValue(userId, out var connectionIds))
            {
                return connectionIds
                    .Select(id => _connectionToUser.TryGetValue(id, out var info) ? info : null)
                    .Where(info => info != null)
                    .ToList()!;
            }
            
            return new List<ProxyConnectionMappingInfo>();
        }
    }
    
    /// <summary>
    /// 获取连接数量
    /// </summary>
    public int ConnectionCount
    {
        get
        {
            lock (_lock)
            {
                return _connectionToUser.Count;
            }
        }
    }
    
    /// <summary>
    /// 获取用户数量
    /// </summary>
    public int UserCount
    {
        get
        {
            lock (_lock)
            {
                return _userToConnections.Count;
            }
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        lock (_lock)
        {
            _connectionToUser.Clear();
            _userToConnections.Clear();
        }
        
        ConsoleWriter.WriteLine("[代理映射] 代理连接映射管理器已关闭");
    }
} 