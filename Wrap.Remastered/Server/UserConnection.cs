using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Server;

/// <summary>
/// 用户连接信息
/// </summary>
public class UserConnection
{
    private volatile bool _isConnected = true;

    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfo UserInfo { get; }

    /// <summary>
    /// 网络套接字
    /// </summary>
    public Socket Socket { get; }

    /// <summary>
    /// 远程端点
    /// </summary>
    public IPEndPoint RemoteEndPoint => Socket.RemoteEndPoint as IPEndPoint ?? new IPEndPoint(IPAddress.None, 0);

    /// <summary>
    /// 本地端点
    /// </summary>
    public IPEndPoint LocalEndPoint => Socket.LocalEndPoint as IPEndPoint ?? new IPEndPoint(IPAddress.None, 0);

    /// <summary>
    /// 连接时间
    /// </summary>
    public DateTime ConnectedAt { get; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivity { get; private set; }

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected && Socket.Connected;
        private set => _isConnected = value;
    }

    /// <summary>
    /// 连接关闭事件
    /// </summary>
    public event EventHandler? ConnectionClosed;

    public UserConnection(UserInfo userInfo, Socket socket)
    {
        UserInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        Socket = socket ?? throw new ArgumentNullException(nameof(socket));
        ConnectedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;

        // 启动连接监控任务
        _ = Task.Run(MonitorConnectionAsync);
    }

    /// <summary>
    /// 更新最后活动时间
    /// </summary>
    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void Close()
    {
        if (IsConnected)
        {
            IsConnected = false;
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task MonitorConnectionAsync()
    {
        try
        {
            while (IsConnected)
            {
                await Task.Delay(5000); // 每5秒检查一次

                // 检查套接字是否仍然连接
                if (!Socket.Connected)
                {
                    Close();
                    break;
                }

                // 检查套接字是否可读（用于检测连接状态）
                if (Socket.Available == 0 && !Socket.Poll(1000, SelectMode.SelectRead))
                {
                    // 尝试发送心跳包或检查连接状态
                    try
                    {
                        // 这里可以添加心跳包逻辑
                        // 暂时只是检查连接状态
                    }
                    catch (Exception)
                    {
                        Close();
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            Close();
        }
    }
}
