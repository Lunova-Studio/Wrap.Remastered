using DotNetty.Transport.Channels;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Server.Managers;
using Wrap.Remastered.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public class RoomKickPacketHandler : RoomPacketHandler
{
    public RoomKickPacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var req = RoomKickPacket.Serializer.Deserialize(packet.Data) as RoomKickPacket;
        if (req == null) return;
        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null) return;
        var ownerConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        if (ownerConn == null || room.Owner.UserId != ownerConn.UserInfo.UserId) return;
        var kickedUser = room.Users.FirstOrDefault(u => u.UserId == req.UserId);
        if (kickedUser == null) return;
        RoomManager.RemoveUserFromRoom(room.Id, req.UserId,
            async (changedRoom, oldOwnerId) =>
            {
                var ownerChangedPacket = new RoomOwnerChangedPacket(changedRoom.Id, changedRoom.Owner.UserId);
                foreach (var u in changedRoom.Users)
                {
                    await Server.GetConnectionManager().SendPacketToUserAsync(u.UserId, ownerChangedPacket);
                }
            },
            async (dismissedRoomId, userIds) =>
            {
                var dismissedPacket = new RoomDismissedPacket(dismissedRoomId);
                foreach (var uid in userIds)
                {
                    await Server.GetConnectionManager().SendPacketToUserAsync(uid, dismissedPacket);
                }
            });
        await Server.GetConnectionManager().SendPacketToUserAsync(req.UserId, new RoomJoinResultPacket(room.Id, false, "您已被房主踢出房间"));
        var infoPacket = new RoomInfoPacket(room);
        foreach (var u in room.Users)
        {
            await Server.GetConnectionManager().SendPacketToUserAsync(u.UserId, infoPacket);
        }
    }
} 