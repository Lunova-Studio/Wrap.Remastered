namespace Wrap.Shared.Events;

/// <summary>
/// 数据接收事件参数
/// </summary>
public sealed class DataReceivedEventArgs : EventArgs {
    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// 接收到的数据
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// 接收时间
    /// </summary>
    public DateTime ReceivedAt { get; }

    public DataReceivedEventArgs(string clientId, byte[] data) {
        ClientId = clientId;
        Data = data;
        ReceivedAt = DateTime.UtcNow;
    }
}