using System;
using System.Net;

namespace Wrap.Remastered.Client.Events;

/// <summary>
/// 客户端事件参数基类
/// </summary>
public class ClientEventArgs : EventArgs
{
    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTime Timestamp { get; }

    public ClientEventArgs()
    {
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// 客户端连接事件参数
/// </summary>
public class ClientConnectedEventArgs : ClientEventArgs
{
    /// <summary>
    /// 服务器端点
    /// </summary>
    public IPEndPoint ServerEndPoint { get; }

    /// <summary>
    /// 连接时间
    /// </summary>
    public DateTime ConnectedAt { get; }

    public ClientConnectedEventArgs(IPEndPoint serverEndPoint, DateTime connectedAt)
    {
        ServerEndPoint = serverEndPoint ?? throw new ArgumentNullException(nameof(serverEndPoint));
        ConnectedAt = connectedAt;
    }
}

/// <summary>
/// 客户端断开事件参数
/// </summary>
public class ClientDisconnectedEventArgs : ClientEventArgs
{
    /// <summary>
    /// 断开原因
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// 连接时长
    /// </summary>
    public TimeSpan ConnectionDuration { get; }

    public ClientDisconnectedEventArgs(string reason, TimeSpan connectionDuration)
    {
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        ConnectionDuration = connectionDuration;
    }
}

/// <summary>
/// 数据接收事件参数
/// </summary>
public class DataReceivedEventArgs : ClientEventArgs
{
    /// <summary>
    /// 接收到的数据
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// 数据长度
    /// </summary>
    public int DataLength { get; }

    public DataReceivedEventArgs(byte[] data)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        DataLength = data.Length;
    }
}

/// <summary>
/// 错误事件参数
/// </summary>
public class ClientErrorEventArgs : ClientEventArgs
{
    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// 异常对象
    /// </summary>
    public Exception? Exception { get; }

    public ClientErrorEventArgs(string errorMessage, Exception? exception = null)
    {
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        Exception = exception;
    }
} 