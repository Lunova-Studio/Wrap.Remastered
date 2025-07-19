using System;
using DotNetty.Transport.Channels;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Server.Handlers;
using Wrap.Remastered.Server.Services;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

/// <summary>
/// 基础数据包处理器
/// </summary>
public abstract class BasePacketHandler : IPacketHandler
{
    protected IWrapServer Server { get; private set; }

    protected BasePacketHandler(IWrapServer server)
    {
        Server = server ?? throw new ArgumentNullException(nameof(server));
    }

    /// <summary>
    /// 处理数据包
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    public virtual void Handle(IChannel channel, UnsolvedPacket packet)
    {
        try
        {
            OnHandle(channel, packet);
        }
        catch (Exception ex)
        {
            Server.GetLoggingService().LogError("Packet", "处理数据包时发生错误", ex, "通道: {0}, 数据包类型: {1}", 
                channel.RemoteAddress, packet.PacketType);
            OnError(channel, packet, ex);
        }
    }

    /// <summary>
    /// 具体的数据包处理逻辑
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    protected abstract void OnHandle(IChannel channel, UnsolvedPacket packet);

    /// <summary>
    /// 错误处理
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    /// <param name="exception">异常</param>
    protected virtual void OnError(IChannel channel, UnsolvedPacket packet, Exception exception)
    {
        Server.GetLoggingService().LogError("Packet", "数据包处理错误", exception, "通道: {0}, 数据包类型: {1}", 
            channel.RemoteAddress, packet.PacketType);
    }

    /// <summary>
    /// 记录处理日志
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    /// <param name="message">日志消息</param>
    protected virtual void LogInfo(IChannel channel, UnsolvedPacket? packet, string message)
    {
        Server.GetLoggingService().LogPacket("{0} - 通道: {1}", message, channel.RemoteAddress);
    }
} 