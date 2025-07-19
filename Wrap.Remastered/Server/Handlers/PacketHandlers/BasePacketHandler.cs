using System;
using DotNetty.Transport.Channels;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Server.Handlers;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

/// <summary>
/// 基础数据包处理器
/// </summary>
public abstract class BasePacketHandler : IPacketHandler
{
    protected readonly IConnectionManager ConnectionManager;

    protected BasePacketHandler(IConnectionManager connectionManager)
    {
        ConnectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
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
            Console.WriteLine($"处理数据包时发生错误: {ex.Message}");
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
        Console.WriteLine($"数据包处理错误: 通道={channel.RemoteAddress}, 数据包类型={packet.PacketType}, 错误={exception.Message}");
    }

    /// <summary>
    /// 记录处理日志
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    /// <param name="message">日志消息</param>
    protected virtual void LogInfo(IChannel channel, UnsolvedPacket packet, string message)
    {
        Console.WriteLine($"[{packet.PacketType}] {channel.RemoteAddress}: {message}");
    }
} 