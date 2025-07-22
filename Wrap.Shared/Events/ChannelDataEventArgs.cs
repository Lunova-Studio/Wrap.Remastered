using Wrap.Shared.Network;

namespace Wrap.Shared.Events;

/// <summary>
/// 通道数据事件参数
/// </summary>
public sealed class ChannelDataEventArgs : EventArgs {
    /// <summary>
    /// 通道连接
    /// </summary>
    public ChannelConnection Connection { get; }

    /// <summary>
    /// 数据
    /// </summary>
    public byte[] Data { get; }

    public ChannelDataEventArgs(ChannelConnection connection, byte[] data) {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }
}