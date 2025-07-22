using DotNetty.Transport.Channels;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Network.Packets;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class RoomTransferOwnerPacketHandler : RoomPacketHandler {
    public RoomTransferOwnerPacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        // 房主转移功能已取消
        var ownerConn = Server.GetConnectionManager()
            .GetAllUserConnections()
            .FirstOrDefault(c => c.Channel == channel);

        if (ownerConn != null) {
            // 发送拒绝通知
            var disconnectPacket = new DisconnectPacket("房主转移功能已取消");
            await Server.GetConnectionManager().SendPacketToUserAsync(ownerConn.UserInfo!.UserId, disconnectPacket);
        }
    }
}