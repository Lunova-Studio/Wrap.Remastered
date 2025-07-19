using System;
using DotNetty.Transport.Channels;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Server.Handlers;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

/// <summary>
/// 未知数据包处理器
/// </summary>
public class UnknownPacketHandler : BasePacketHandler
{
    public UnknownPacketHandler(IConnectionManager connectionManager) : base(connectionManager)
    {
    }

    protected override void OnHandle(IChannel channel, UnsolvedPacket packet)
    {
        LogInfo(channel, packet, $"收到未知数据包类型: {packet.PacketType}, 数据长度: {packet.Data.Length} 字节");
        
        // 可以选择记录数据包内容用于调试
        if (packet.Data.Length > 0)
        {
            var hexData = BitConverter.ToString(packet.Data).Replace("-", " ");
            LogInfo(channel, packet, $"数据包内容: {hexData}");
        }
    }

    protected override void OnError(IChannel channel, UnsolvedPacket packet, Exception exception)
    {
        LogInfo(channel, packet, $"处理未知数据包时发生错误: {exception.Message}");
    }
} 