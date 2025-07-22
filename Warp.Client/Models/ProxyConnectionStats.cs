namespace Warp.Client.Models;

/// <summary>
/// 代理连接统计信息
/// </summary>
public record ProxyConnectionStats {
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
    public string ConnectionStatus {
        get {
            if (ActiveConnections == 0) 
                return "无活跃连接";

            if (ActiveConnections >= MaxConnections) 
                return "连接已满";

            return $"正常 ({ActiveConnections}/{MaxConnections})";
        }
    }
}