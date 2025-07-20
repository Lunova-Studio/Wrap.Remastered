using System;
using System.Collections.Generic;
using System.Linq;

namespace Wrap.Remastered.Client;

/// <summary>
/// 代理连接统计信息
/// </summary>
public class ProxyConnectionStats
{
    /// <summary>
    /// 总连接数
    /// </summary>
    public int TotalConnections { get; set; }
    
    /// <summary>
    /// 活跃连接数
    /// </summary>
    public int ActiveConnections { get; set; }
    
    /// <summary>
    /// 最大连接数限制
    /// </summary>
    public int MaxConnections { get; set; }
    
    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int ConnectionTimeout { get; set; }
    
    /// <summary>
    /// 连接利用率（百分比）
    /// </summary>
    public double ConnectionUtilization => MaxConnections > 0 ? (double)ActiveConnections / MaxConnections * 100 : 0;
    
    /// <summary>
    /// 连接状态描述
    /// </summary>
    public string ConnectionStatus
    {
        get
        {
            if (ActiveConnections == 0) return "无活跃连接";
            if (ActiveConnections >= MaxConnections) return "连接已满";
            return $"正常 ({ActiveConnections}/{MaxConnections})";
        }
    }
}

/// <summary>
/// 代理映射统计信息
/// </summary>
public class ProxyMappingStats
{
    /// <summary>
    /// 总连接数
    /// </summary>
    public int TotalConnections { get; set; }
    
    /// <summary>
    /// 活跃连接数
    /// </summary>
    public int ActiveConnections { get; set; }
    
    /// <summary>
    /// 总用户数
    /// </summary>
    public int TotalUsers { get; set; }
    
    /// <summary>
    /// 总传输字节数
    /// </summary>
    public long TotalBytesTransferred { get; set; }
    
    /// <summary>
    /// 平均每个连接的传输字节数
    /// </summary>
    public double AverageBytesPerConnection => TotalConnections > 0 ? (double)TotalBytesTransferred / TotalConnections : 0;
    
    /// <summary>
    /// 平均每个用户的连接数
    /// </summary>
    public double AverageConnectionsPerUser => TotalUsers > 0 ? (double)TotalConnections / TotalUsers : 0;
    
    /// <summary>
    /// 传输字节数的人类可读格式
    /// </summary>
    public string TotalBytesTransferredFormatted
    {
        get
        {
            if (TotalBytesTransferred < 1024) return $"{TotalBytesTransferred} B";
            if (TotalBytesTransferred < 1024 * 1024) return $"{TotalBytesTransferred / 1024.0:F1} KB";
            if (TotalBytesTransferred < 1024 * 1024 * 1024) return $"{TotalBytesTransferred / (1024.0 * 1024):F1} MB";
            return $"{TotalBytesTransferred / (1024.0 * 1024 * 1024):F1} GB";
        }
    }
}

/// <summary>
/// 本地代理服务器统计信息
/// </summary>
public class LocalProxyStats
{
    /// <summary>
    /// 服务器运行状态
    /// </summary>
    public bool IsRunning { get; set; }
    
    /// <summary>
    /// 本地监听端口
    /// </summary>
    public int LocalPort { get; set; }
    
    /// <summary>
    /// 目标地址
    /// </summary>
    public string TargetAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 目标端口
    /// </summary>
    public int TargetPort { get; set; }
    
    /// <summary>
    /// 连接统计
    /// </summary>
    public ProxyConnectionStats ConnectionStats { get; set; } = new();
    
    /// <summary>
    /// 映射统计
    /// </summary>
    public ProxyMappingStats MappingStats { get; set; } = new();
    
    /// <summary>
    /// 服务器状态描述
    /// </summary>
    public string ServerStatus => IsRunning ? "运行中" : "已停止";
    
    /// <summary>
    /// 目标地址描述
    /// </summary>
    public string TargetDescription => $"{TargetAddress}:{TargetPort}";
}

/// <summary>
/// 房主代理管理器统计信息
/// </summary>
public class ProxyManagerStats
{
    /// <summary>
    /// 默认目标地址
    /// </summary>
    public string DefaultTargetAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 默认目标端口
    /// </summary>
    public int DefaultTargetPort { get; set; }
    
    /// <summary>
    /// 连接统计
    /// </summary>
    public ProxyConnectionStats ConnectionStats { get; set; } = new();
    
    /// <summary>
    /// 映射统计
    /// </summary>
    public ProxyMappingStats MappingStats { get; set; } = new();
    
    /// <summary>
    /// 默认目标描述
    /// </summary>
    public string DefaultTargetDescription => $"{DefaultTargetAddress}:{DefaultTargetPort}";
}

/// <summary>
/// 综合代理统计信息
/// </summary>
public class ProxyStatistics
{
    /// <summary>
    /// 统计时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 本地代理统计
    /// </summary>
    public LocalProxyStats? LocalProxyStats { get; set; }
    
    /// <summary>
    /// 房主代理统计
    /// </summary>
    public ProxyManagerStats? ProxyManagerStats { get; set; }
    
    /// <summary>
    /// 总连接数
    /// </summary>
    public int TotalConnections => 
        (LocalProxyStats?.ConnectionStats.TotalConnections ?? 0) + 
        (ProxyManagerStats?.ConnectionStats.TotalConnections ?? 0);
    
    /// <summary>
    /// 总活跃连接数
    /// </summary>
    public int TotalActiveConnections => 
        (LocalProxyStats?.ConnectionStats.ActiveConnections ?? 0) + 
        (ProxyManagerStats?.ConnectionStats.ActiveConnections ?? 0);
    
    /// <summary>
    /// 总传输字节数
    /// </summary>
    public long TotalBytesTransferred => 
        (LocalProxyStats?.MappingStats.TotalBytesTransferred ?? 0) + 
        (ProxyManagerStats?.MappingStats.TotalBytesTransferred ?? 0);
    
    /// <summary>
    /// 总用户数
    /// </summary>
    public int TotalUsers => 
        (LocalProxyStats?.MappingStats.TotalUsers ?? 0) + 
        (ProxyManagerStats?.MappingStats.TotalUsers ?? 0);
    
    /// <summary>
    /// 总体状态描述
    /// </summary>
    public string OverallStatus
    {
        get
        {
            if (TotalActiveConnections == 0) return "无活跃连接";
            if (LocalProxyStats?.IsRunning == true) return "本地代理运行中";
            if (ProxyManagerStats != null) return "房主代理运行中";
            return "代理未运行";
        }
    }
    
    /// <summary>
    /// 总传输字节数的人类可读格式
    /// </summary>
    public string TotalBytesTransferredFormatted
    {
        get
        {
            if (TotalBytesTransferred < 1024) return $"{TotalBytesTransferred} B";
            if (TotalBytesTransferred < 1024 * 1024) return $"{TotalBytesTransferred / 1024.0:F1} KB";
            if (TotalBytesTransferred < 1024 * 1024 * 1024) return $"{TotalBytesTransferred / (1024.0 * 1024):F1} MB";
            return $"{TotalBytesTransferred / (1024.0 * 1024 * 1024):F1} GB";
        }
    }
    
    /// <summary>
    /// 创建本地代理统计信息
    /// </summary>
    /// <param name="isRunning">是否运行</param>
    /// <param name="localPort">本地端口</param>
    /// <param name="targetAddress">目标地址</param>
    /// <param name="targetPort">目标端口</param>
    /// <param name="connectionStats">连接统计</param>
    /// <param name="mappingStats">映射统计</param>
    /// <returns>本地代理统计信息</returns>
    public static LocalProxyStats CreateLocalProxyStats(
        bool isRunning, 
        int localPort, 
        string targetAddress, 
        int targetPort,
        ProxyConnectionStats connectionStats,
        ProxyMappingStats mappingStats)
    {
        return new LocalProxyStats
        {
            IsRunning = isRunning,
            LocalPort = localPort,
            TargetAddress = targetAddress,
            TargetPort = targetPort,
            ConnectionStats = connectionStats,
            MappingStats = mappingStats
        };
    }
    
    /// <summary>
    /// 创建房主代理统计信息
    /// </summary>
    /// <param name="defaultTargetAddress">默认目标地址</param>
    /// <param name="defaultTargetPort">默认目标端口</param>
    /// <param name="connectionStats">连接统计</param>
    /// <param name="mappingStats">映射统计</param>
    /// <returns>房主代理统计信息</returns>
    public static ProxyManagerStats CreateProxyManagerStats(
        string defaultTargetAddress,
        int defaultTargetPort,
        ProxyConnectionStats connectionStats,
        ProxyMappingStats mappingStats)
    {
        return new ProxyManagerStats
        {
            DefaultTargetAddress = defaultTargetAddress,
            DefaultTargetPort = defaultTargetPort,
            ConnectionStats = connectionStats,
            MappingStats = mappingStats
        };
    }
    
    /// <summary>
    /// 从字典创建连接统计信息
    /// </summary>
    /// <param name="stats">统计字典</param>
    /// <returns>连接统计信息</returns>
    public static ProxyConnectionStats CreateConnectionStats(Dictionary<string, object> stats)
    {
        return new ProxyConnectionStats
        {
            TotalConnections = GetValueOrDefault(stats, "TotalConnections", 0),
            ActiveConnections = GetValueOrDefault(stats, "ActiveConnections", 0),
            MaxConnections = GetValueOrDefault(stats, "MaxConnections", 0),
            ConnectionTimeout = GetValueOrDefault(stats, "ConnectionTimeout", 0)
        };
    }
    
    /// <summary>
    /// 从字典创建映射统计信息
    /// </summary>
    /// <param name="stats">统计字典</param>
    /// <returns>映射统计信息</returns>
    public static ProxyMappingStats CreateMappingStats(Dictionary<string, object> stats)
    {
        return new ProxyMappingStats
        {
            TotalConnections = GetValueOrDefault(stats, "TotalConnections", 0),
            ActiveConnections = GetValueOrDefault(stats, "ActiveConnections", 0),
            TotalUsers = GetValueOrDefault(stats, "TotalUsers", 0),
            TotalBytesTransferred = GetValueOrDefault(stats, "TotalBytesTransferred", 0L)
        };
    }
    
    /// <summary>
    /// 从字典获取值，如果不存在则返回默认值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="stats">统计字典</param>
    /// <param name="key">键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>值或默认值</returns>
    private static T GetValueOrDefault<T>(Dictionary<string, object> stats, string key, T defaultValue)
    {
        if (stats.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
    
    /// <summary>
    /// 获取详细的统计报告
    /// </summary>
    /// <returns>统计报告字符串</returns>
    public string GetDetailedReport()
    {
        var report = new List<string>
        {
            $"=== 代理统计报告 ({Timestamp:yyyy-MM-dd HH:mm:ss}) ===",
            $"总体状态: {OverallStatus}",
            $"总连接数: {TotalConnections}",
            $"总活跃连接数: {TotalActiveConnections}",
            $"总用户数: {TotalUsers}",
            $"总传输字节数: {TotalBytesTransferredFormatted}",
            ""
        };
        
        if (LocalProxyStats != null)
        {
            report.AddRange(new[]
            {
                "--- 本地代理统计 ---",
                $"运行状态: {LocalProxyStats.ServerStatus}",
                $"监听端口: {LocalProxyStats.LocalPort}",
                $"目标地址: {LocalProxyStats.TargetDescription}",
                $"连接状态: {LocalProxyStats.ConnectionStats.ConnectionStatus}",
                $"连接利用率: {LocalProxyStats.ConnectionStats.ConnectionUtilization:F1}%",
                $"映射连接数: {LocalProxyStats.MappingStats.TotalConnections}",
                $"映射用户数: {LocalProxyStats.MappingStats.TotalUsers}",
                $"映射传输字节数: {LocalProxyStats.MappingStats.TotalBytesTransferredFormatted}",
                ""
            });
        }
        
        if (ProxyManagerStats != null)
        {
            report.AddRange(new[]
            {
                "--- 房主代理统计 ---",
                $"默认目标: {ProxyManagerStats.DefaultTargetDescription}",
                $"连接状态: {ProxyManagerStats.ConnectionStats.ConnectionStatus}",
                $"连接利用率: {ProxyManagerStats.ConnectionStats.ConnectionUtilization:F1}%",
                $"映射连接数: {ProxyManagerStats.MappingStats.TotalConnections}",
                $"映射用户数: {ProxyManagerStats.MappingStats.TotalUsers}",
                $"映射传输字节数: {ProxyManagerStats.MappingStats.TotalBytesTransferredFormatted}",
                ""
            });
        }
        
        report.Add("=== 报告结束 ===");
        
        return string.Join(Environment.NewLine, report);
    }
} 