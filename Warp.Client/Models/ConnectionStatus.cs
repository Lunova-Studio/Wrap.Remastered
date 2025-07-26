namespace Warp.Client.Models;

/// <summary>
/// P2P连接状态信息
/// </summary>
public record ConnectionStatus
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// 目标用户ID
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>
    /// 最后心跳时间
    /// </summary>
    public DateTime LastKeepAlive { get; set; }

    /// <summary>
    /// 最后发送心跳时间
    /// </summary>
    public DateTime LastSentKeepAlive { get; set; }

    /// <summary>
    /// 是否有待处理的心跳
    /// </summary>
    public bool HasPendingKeepAlive { get; set; }

    /// <summary>
    /// 期望的响应值
    /// </summary>
    public int? ExpectedResponse { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 连接延迟（毫秒）
    /// </summary>
    public int? LatencyMs { get; set; }

    /// <summary>
    /// 连接持续时间
    /// </summary>
    public TimeSpan? ConnectionDuration { get; set; }

    /// <summary>
    /// 获取连接状态描述
    /// </summary>
    /// <returns>状态描述</returns>
    public string GetStatusDescription()
    {
        if (!string.IsNullOrEmpty(Error))
        {
            return $"错误: {Error}";
        }

        if (!IsConnected)
        {
            return "未连接";
        }

        var status = "已连接";

        if (HasPendingKeepAlive)
        {
            status += " (等待心跳响应)";
        }

        if (LatencyMs.HasValue)
        {
            status += $" (延迟: {LatencyMs.Value}ms)";
        }

        return status;
    }

    /// <summary>
    /// 获取最后心跳时间的描述
    /// </summary>
    /// <returns>时间描述</returns>
    public string GetLastKeepAliveDescription()
    {
        if (LastKeepAlive == default)
        {
            return "从未";
        }

        var timeSpan = DateTime.UtcNow - LastKeepAlive;
        if (timeSpan.TotalSeconds < 60)
            return $"{(int)timeSpan.TotalSeconds} 秒前";
        else if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}分钟前";
        else
            return $"{(int)timeSpan.TotalHours} 小时前";
    }

    public TimeSpan GetPing()
    {
        if (IsConnected)
        {
            if (LastSentKeepAlive == default || LastKeepAlive == default)
                return TimeSpan.Zero;
            return LastKeepAlive - LastSentKeepAlive;
        }

        return TimeSpan.Zero;
    }
}