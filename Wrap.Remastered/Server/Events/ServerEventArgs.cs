using System;

namespace Wrap.Remastered.Server.Events;

/// <summary>
/// 服务器启动事件参数
/// </summary>
public class ServerStartedEventArgs : EventArgs
{
    /// <summary>
    /// 启动时间
    /// </summary>
    public DateTime StartedAt { get; }

    /// <summary>
    /// 服务器端口
    /// </summary>
    public int Port { get; }

    public ServerStartedEventArgs(int port)
    {
        StartedAt = DateTime.UtcNow;
        Port = port;
    }
}

/// <summary>
/// 服务器停止事件参数
/// </summary>
public class ServerStoppedEventArgs : EventArgs
{
    /// <summary>
    /// 停止时间
    /// </summary>
    public DateTime StoppedAt { get; }

    /// <summary>
    /// 停止原因
    /// </summary>
    public string? Reason { get; }

    public ServerStoppedEventArgs(string? reason = null)
    {
        StoppedAt = DateTime.UtcNow;
        Reason = reason;
    }
}

/// <summary>
/// 客户端连接事件参数
/// </summary>
public class ClientConnectedEventArgs : EventArgs
{
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

    public ClientConnectedEventArgs(string clientId, string remoteAddress)
    {
        ClientId = clientId;
        RemoteAddress = remoteAddress;
        ConnectedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// 客户端断开事件参数
/// </summary>
public class ClientDisconnectedEventArgs : EventArgs
{
    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// 断开时间
    /// </summary>
    public DateTime DisconnectedAt { get; }

    /// <summary>
    /// 断开原因
    /// </summary>
    public string? Reason { get; }

    public ClientDisconnectedEventArgs(string clientId, string? reason = null)
    {
        ClientId = clientId;
        DisconnectedAt = DateTime.UtcNow;
        Reason = reason;
    }
}

/// <summary>
/// 数据接收事件参数
/// </summary>
public class DataReceivedEventArgs : EventArgs
{
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

    public DataReceivedEventArgs(string clientId, byte[] data)
    {
        ClientId = clientId;
        Data = data;
        ReceivedAt = DateTime.UtcNow;
    }
} 