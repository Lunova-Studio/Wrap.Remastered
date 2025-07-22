using DotNetty.Transport.Channels;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Remastered.Server.Managers;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class RoomDismissPacketHandler : RoomPacketHandler {
    public RoomDismissPacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (RoomDismissPacket.Serializer.Deserialize(packet.Data) is not RoomDismissPacket req)
            return;

        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null) 
            return;

        var ownerConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        if (ownerConn == null || room.Owner.UserId != ownerConn.UserInfo!.UserId)
            return;

        var userIds = room.Users.Select(u => u.Key).ToList();
        RoomManager.RemoveRoom(room.Id);
        var dismissedPacket = new RoomDismissedPacket(room.Id);

        foreach (var uid in userIds)
            await Server.GetConnectionManager().SendPacketToUserAsync(uid, dismissedPacket);
    }
}