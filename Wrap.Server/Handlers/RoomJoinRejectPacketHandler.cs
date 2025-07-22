using DotNetty.Transport.Channels;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Remastered.Server.Managers;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class RoomJoinRejectPacketHandler : RoomPacketHandler {
    public RoomJoinRejectPacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (RoomJoinRejectPacket.Serializer.Deserialize(packet.Data) is not RoomJoinRejectPacket req)
            return;

        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null)
            return;

        var ownerConn = Server.GetConnectionManager()
            .GetAllUserConnections()
            .FirstOrDefault(c => c.Channel == channel);

        if (ownerConn == null || room.Owner.UserId != ownerConn.UserInfo.UserId)
            return;
        await Server.GetConnectionManager()
            .SendPacketToUserAsync(req.UserId, new RoomJoinResultPacket(room.Id, false, "房主拒绝了你的加入请求"));
    }
}