using DotNetty.Transport.Channels;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Remastered.Server.Managers;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class RoomKickPacketHandler : RoomPacketHandler {
    public RoomKickPacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (RoomKickPacket.Serializer.Deserialize(packet.Data) is not RoomKickPacket req)
            return;

        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null)
            return;

        var ownerConn = Server.GetConnectionManager()
            .GetAllUserConnections()
            .FirstOrDefault(c => c.Channel == channel);

        if (ownerConn?.UserInfo == null || room.Owner.UserId != ownerConn.UserInfo.UserId)
            return;

        var kickedUser = room.Users.TryGetValue(req.UserId, out var userToKick) ? userToKick : null;
        if (kickedUser == null)
            return;

        RoomManager.RemoveUserFromRoom(room.Id, req.UserId,
            async (dismissedRoomId, userIds) => {
                var dismissedPacket = new RoomDismissedPacket(dismissedRoomId);
                foreach (var uid in userIds) {
                    await Server.GetConnectionManager().SendPacketToUserAsync(uid, dismissedPacket);
                }
            });

        await Server.GetConnectionManager().SendPacketToUserAsync(
            req.UserId,
            new RoomJoinResultPacket(room.Id, success: false, message: "您已被房主踢出房间")
        );

        var infoPacket = new RoomInfoPacket(room);
        foreach (var user in room.Users) {
            await Server.GetConnectionManager().SendPacketToUserAsync(user.Key, infoPacket);
        }
    }
}
