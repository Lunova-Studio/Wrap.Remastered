using DotNetty.Transport.Channels;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Interfaces;

/// <summary>
/// 基础数据包处理器
/// </summary>
public interface IPacketHandler {
    Task OnHandleAsync(IChannel channel, UnsolvedPacket packet);
}