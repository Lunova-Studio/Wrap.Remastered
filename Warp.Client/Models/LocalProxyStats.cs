namespace Warp.Client.Models;

/// <summary>
/// 本地代理服务器统计信息
/// </summary>
public record LocalProxyStats {
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