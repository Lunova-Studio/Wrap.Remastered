namespace Wrap.Shared.Events;

/// <summary>
/// 服务器启动事件参数
/// </summary>
public sealed class ServerStartedEventArgs : EventArgs {
    /// <summary>
    /// 启动时间
    /// </summary>
    public DateTime StartedAt { get; }

    /// <summary>
    /// 服务器端口
    /// </summary>
    public int Port { get; }

    public ServerStartedEventArgs(int port) {
        StartedAt = DateTime.UtcNow;
        Port = port;
    }
}