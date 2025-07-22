namespace Warp.Client.Models;

/// <summary>
/// 代理连接映射信息
/// </summary>
public record ProxyConnectionMappingInfo {
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