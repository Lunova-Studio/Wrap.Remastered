using System.Net.Sockets;

namespace Warp.Client.Models;

/// <summary>
/// 本地代理连接信息
/// </summary>
public record LocalProxyConnectionInfo {
    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// 本地TCP客户端
    /// </summary>
    public TcpClient? LocalClient { get; set; }

    /// <summary>
    /// 本地网络流
    /// </summary>
    public NetworkStream? LocalStream { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public bool IsConnected => LocalClient?.Connected == true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public long SequenceIdCounter = 0;
}