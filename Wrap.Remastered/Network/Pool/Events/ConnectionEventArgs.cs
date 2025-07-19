using System;
using System.Net;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Network.Pool.Events;

/// <summary>
/// 连接事件参数基类
/// </summary>
public class ConnectionEventArgs : EventArgs
{
    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTime Timestamp { get; }

    public ConnectionEventArgs()
    {
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// 用户连接事件参数
/// </summary>
public class UserConnectionEventArgs : ConnectionEventArgs
{
    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfo UserInfo { get; }

    /// <summary>
    /// 远程端点
    /// </summary>
    public IPEndPoint RemoteEndPoint { get; }

    public UserConnectionEventArgs(UserInfo userInfo, IPEndPoint remoteEndPoint)
    {
        UserInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
    }
}

/// <summary>
/// 连接统计事件参数
/// </summary>
public class ConnectionStatisticsEventArgs : ConnectionEventArgs
{
    /// <summary>
    /// 连接统计信息
    /// </summary>
    public NetworkPoolStatistics Statistics { get; }

    public ConnectionStatisticsEventArgs(NetworkPoolStatistics statistics)
    {
        Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
    }
}

/// <summary>
/// 连接池满事件参数
/// </summary>
public class ConnectionPoolFullEventArgs : ConnectionEventArgs
{
    /// <summary>
    /// 尝试连接的用户ID
    /// </summary>
    public string? AttemptingUserId { get; }

    /// <summary>
    /// 尝试连接的端点
    /// </summary>
    public IPEndPoint? AttemptingEndPoint { get; }

    public ConnectionPoolFullEventArgs(string? attemptingUserId = null, IPEndPoint? attemptingEndPoint = null)
    {
        AttemptingUserId = attemptingUserId;
        AttemptingEndPoint = attemptingEndPoint;
    }
} 