using System.Collections.Concurrent;
using System.Net.Sockets;
using Wrap.Shared.Network.Packets;

namespace Warp.Client.Models;

/// <summary>
/// 代理连接信息
/// </summary>
public record ProxyConnectionInfo {
    public Task? PeerToTargetTask;
    public long SequenceIdCounter = 0;
    public CancellationTokenSource PeerToTargetCts = new();
    // 新增：peer->目标服务器方向的队列和写入任务
    public ConcurrentQueue<ProxyDataPacket> PeerToTargetQueue = new();

    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// 目标TCP客户端
    /// </summary>
    public TcpClient? TargetClient { get; set; }

    /// <summary>
    /// 目标网络流
    /// </summary>
    public NetworkStream? TargetStream { get; set; }

    /// <summary>
    /// 目标地址
    /// </summary>
    public string TargetAddress { get; set; } = string.Empty;

    /// <summary>
    /// 目标端口
    /// </summary>
    public int TargetPort { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public bool IsConnected => TargetClient?.Connected == true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public SemaphoreSlim WriteLock { get; } = new(1, 1);
}