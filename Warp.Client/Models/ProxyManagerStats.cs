namespace Warp.Client.Models;

/// <summary>
/// 房主代理管理器统计信息
/// </summary>
public record ProxyManagerStats {
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