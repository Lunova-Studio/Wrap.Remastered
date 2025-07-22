using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrap.Shared.Events;

/// <summary>
/// 服务器停止事件参数
/// </summary>
public sealed class ServerStoppedEventArgs : EventArgs {
    /// <summary>
    /// 停止时间
    /// </summary>
    public DateTime StoppedAt { get; }

    /// <summary>
    /// 停止原因
    /// </summary>
    public string? Reason { get; }

    public ServerStoppedEventArgs(string? reason = null) {
        StoppedAt = DateTime.UtcNow;
        Reason = reason;
    }
}