using Wrap.Shared.Network;

namespace Wrap.Shared.Events;

/// <summary>
/// 通道连接事件参数
/// </summary>
public sealed class ChannelConnectionEventArgs : EventArgs {
    /// <summary>
    /// 通道连接
    /// </summary>
    public ChannelConnection Connection { get; }

    public ChannelConnectionEventArgs(ChannelConnection connection) {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
}