using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;

namespace Wrap.Shared.Network;

/// <summary>
/// 通道连接
/// </summary>
public class ChannelConnection {
    private readonly IChannel _channel;
    private UserInfo? _userInfo;
    private DateTime _lastActivity;
    private int? _expectedKeepAliveValue; // 期望的 KeepAlive 响应值
    private DateTime _keepAliveSentTime; // 上次发送 KeepAlive 的时间
    private DateTime _keepAliveReceivedTime; // 上次接收 KeepAlive 响应的时间
    private bool _pinging;

    /// <summary>
    /// 通道
    /// </summary>
    public IChannel Channel { get; }

    /// <summary>
    /// 连接时间
    /// </summary>
    public DateTime ConnectedAt { get; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivity {
        get => _lastActivity;
        set => _lastActivity = value;
    }

    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfo? UserInfo => _userInfo;

    /// <summary>
    /// 用户ID
    /// </summary>
    public string? UserId => _userInfo?.UserId;

    /// <summary>
    /// 是否活跃
    /// </summary>
    public bool IsActive => Channel.Active;

    /// <summary>
    /// 远程地址
    /// </summary>
    public string RemoteAddress => Channel.RemoteAddress?.ToString() ?? "Unknown";

    /// <summary>
    /// 期望的KeepAlive响应值
    /// </summary>
    public int? ExpectedKeepAliveValue => _expectedKeepAliveValue;
    /// <summary>
    /// 上次发送KeepAlive的时间
    /// </summary>
    public DateTime KeepAliveSentTime => _keepAliveSentTime;
    /// <summary>
    /// 上次接收KeepAlive响应的时间
    /// </summary>
    public DateTime KeepAliveReceivedTime => _keepAliveReceivedTime;
    public bool Pinging => _pinging;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="channel">通道</param>
    public ChannelConnection(IChannel channel) {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        Channel = channel;
        ConnectedAt = DateTime.UtcNow;
        _lastActivity = ConnectedAt;
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    public void SetUserInfo(UserInfo userInfo) {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        UpdateActivity();
    }

    /// <summary>
    /// 更新活动时间
    /// </summary>
    public void UpdateActivity() {
        _lastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置期望的KeepAlive响应值
    /// </summary>
    /// <param name="value">期望的KeepAlive响应值</param>
    public void SetExpectedKeepAliveValue(int value) {
        _expectedKeepAliveValue = value;
    }

    public void SetKeepAliveSentTime(DateTime time)
    {
        _keepAliveSentTime = time;
    }

    public void SetKeepAliveReceivedTime(DateTime time)
    {
        _keepAliveReceivedTime = time;
    }

    public void SetPinging(bool pinging)
    {
        _pinging = pinging;
    }

    /// <summary>
    /// 验证KeepAlive响应
    /// </summary>
    /// <param name="responseValue">接收到的KeepAlive响应值</param>
    /// <returns>是否匹配</returns>
    public bool ValidateKeepAliveResponse(int responseValue) {
        if (_expectedKeepAliveValue.HasValue && _expectedKeepAliveValue.Value == responseValue) {
            _expectedKeepAliveValue = null; // 清除期望值
            UpdateActivity();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>是否成功发送</returns>
    public async Task<bool> SendDataAsync(byte[] data) {
        if (!IsActive || data == null || data.Length == 0)
            return false;

        try {
            var buffer = Unpooled.WrappedBuffer(data);
            await Channel.WriteAndFlushAsync(buffer);
            UpdateActivity();
            return true;
        } catch (Exception) {
            return false;
        }
    }

    public async Task<bool> SendPacketAsync(IClientBoundPacket packet) {
        try {
            var serializer = packet.GetSerializer();
            var packetData = serializer.Serialize(packet);
            packet.OnSerialize(ref packetData);

            using MemoryStream stream = new MemoryStream(4 + packetData.Length);
            stream.WriteInt32((int)packet.GetPacketType());
            await stream.WriteAsync(packetData, 0, packetData.Length);

            var buffer = Unpooled.WrappedBuffer(stream.GetBuffer());
            await Channel.WriteAndFlushAsync(buffer);

            return true;
        } catch (Exception) {
            return false;
        }
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void Close() {
        try {
            if (IsActive)
                _ = Channel.CloseAsync();
        } catch (Exception) {}
    }

    public TimeSpan GetPing()
    {
        if (Pinging) return TimeSpan.Zero;

        return (_keepAliveReceivedTime - _keepAliveSentTime) / 2;
    }
}