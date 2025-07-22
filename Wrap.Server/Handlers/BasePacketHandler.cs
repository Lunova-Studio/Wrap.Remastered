using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Remastered.Server.Managers;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers;

public abstract class BasePacketHandler : IPacketHandler {
    protected IServerCoordinator Server { get; private set; }

    protected BasePacketHandler(IServerCoordinator server) {
        Server = server ?? throw new ArgumentNullException(nameof(server));
    }

    /// <summary>
    /// 数据包处理逻辑
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    public abstract Task OnHandleAsync(IChannel channel, UnsolvedPacket packet);

    /// <summary>
    /// 错误处理
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    /// <param name="exception">异常</param>
    protected virtual void OnError(IChannel channel, UnsolvedPacket packet, Exception exception) {
        Server.Logger.LogError(exception, "数据包处理错误, 通道: {RemoteAddress}, 数据包类型: {PacketType}",
            channel.RemoteAddress, packet.PacketType);
    }

    /// <summary>
    /// 记录处理日志
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    /// <param name="message">日志消息</param>
    protected virtual void LogInfo(IChannel channel, UnsolvedPacket? packet, string message) {
        Server.Logger.LogInformation("{message} - 通道: {RemoteAddress}", message, channel.RemoteAddress);
    }
}

public abstract class RoomPacketHandler : BasePacketHandler {
    protected RoomManager RoomManager => Server.GetRoomManager();

    protected RoomPacketHandler(IServerCoordinator server) : base(server) { }
}