using System.Net.Sockets;

namespace Warp.Client.Models;

/// <summary>
/// P2P连接信息
/// </summary>
public record PeerConnectionInfo {
    /// <summary>
    /// 目标用户ID
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>
    /// TCP客户端连接
    /// </summary>
    public TcpClient? TcpClient { get; set; }

    /// <summary>
    /// 网络流
    /// </summary>
    public NetworkStream? NetworkStream { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public bool IsConnected => TcpClient?.Connected == true;

    /// <summary>
    /// 最后心跳时间
    /// </summary>
    public DateTime LastKeepAlive { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 期望的心跳响应值
    /// </summary>
    public int? ExpectedKeepAliveResponse { get; set; }

    /// <summary>
    /// 最后发送心跳的时间
    /// </summary>
    public DateTime LastSentKeepAlive { get; set; } = DateTime.UtcNow;

    public SemaphoreSlim WriteLock { get; } = new(1, 1);
}