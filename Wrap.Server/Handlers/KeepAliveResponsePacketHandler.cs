using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class KeepAliveResponsePacketHandler : RoomPacketHandler {
    public KeepAliveResponsePacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (KeepAliveResponsePacket.Serializer.Deserialize(packet.Data) is not KeepAliveResponsePacket req) 
            return;

        var connection = Server.GetConnectionManager()
            .GetChannelConnection(channel);

        if (connection == null)
            return;

        // 验证KeepAlive响应值
        if (connection.ValidateKeepAliveResponse(req.Value)) {
            // 验证成功，连接保持活跃
            Server.Logger.LogDebug("KeepAlive 验证成功: {RemoteAddress}", connection.RemoteAddress);
        } else {
            // 验证失败，断开连接
            Server.Logger.LogWarning("KeepAlive 验证失败，断开连接: {RemoteAddress}", connection.RemoteAddress);
            await Server.GetConnectionManager().DisconnectUserAsync(connection.UserId ?? "", "KeepAlive 验证失败");
        }
    }
}