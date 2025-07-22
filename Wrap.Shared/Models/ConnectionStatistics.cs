namespace Wrap.Shared.Models;

/// <summary>
/// 连接统计信息
/// </summary>
public class ConnectionStatistics {
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