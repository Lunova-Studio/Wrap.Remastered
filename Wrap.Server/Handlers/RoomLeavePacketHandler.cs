using DotNetty.Transport.Channels;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Remastered.Server.Managers;
using Wrap.Shared.Network.Packets;
using Wrap.Shared.Network.Packets.Client;
using Wrap.Shared.Network.Packets.Server;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class RoomLeavePacketHandler : RoomPacketHandler {
    public RoomLeavePacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (RoomLeavePacket.Serializer.Deserialize(packet.Data) is not RoomLeavePacket req)
            return;

        var userConn = Server.GetConnectionManager()
            .GetAllUserConnections()
            .FirstOrDefault(c => c.Channel == channel);

        var userInfo = userConn?.UserInfo;
        if (userInfo == null)
            return;

        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null)
            return;

        RoomManager.RemoveUserFromRoom(room.Id, userInfo.UserId,
            async (dismissedRoomId, userIds) => {
                // 通知所有成员房间解散
                var dismissedPacket = new RoomDismissedPacket(dismissedRoomId);
                foreach (var uid in userIds) {
                    await Server.GetConnectionManager().SendPacketToUserAsync(uid, dismissedPacket);
                }
            });

        // 广播成员变动
        var infoPacket = new RoomInfoPacket(room);
        foreach (var u in room.Users)
            await Server.GetConnectionManager().SendPacketToUserAsync(u.Key, infoPacket);
    }
}