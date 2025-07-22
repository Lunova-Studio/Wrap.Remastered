namespace Wrap.Shared.Events;

/// <summary>
/// 客户端连接事件参数
/// </summary>
public sealed class ClientConnectedEventArgs : EventArgs {
    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// 远程地址
    /// </summary>
    public string RemoteAddress { get; }

    /// <summary>
    /// 连接时间
    /// </summary>
    public DateTime ConnectedAt { get; }

    public ClientConnectedEventArgs(string clientId, string remoteAddress) {
        ClientId = clientId;
        RemoteAddress = remoteAddress;
        ConnectedAt = DateTime.UtcNow;
    }
}