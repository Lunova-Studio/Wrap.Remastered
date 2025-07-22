namespace Wrap.Shared.Events;

/// <summary>
/// 客户端断开事件参数
/// </summary>
public sealed class ClientDisconnectedEventArgs : EventArgs {
    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// 断开时间
    /// </summary>
    public DateTime DisconnectedAt { get; }

    /// <summary>
    /// 断开原因
    /// </summary>
    public string? Reason { get; }

    public ClientDisconnectedEventArgs(string clientId, string? reason = null) {
        ClientId = clientId;
        DisconnectedAt = DateTime.UtcNow;
        Reason = reason;
    }
}