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

public class RoomLeavePacketHandler : RoomPacketHandler
{
    public RoomLeavePacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var req = RoomLeavePacket.Serializer.Deserialize(packet.Data) as RoomLeavePacket;
        if (req == null) return;
        var userConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        var userInfo = userConn?.UserInfo;
        if (userInfo == null) return;
        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null) return;
        RoomManager.RemoveUserFromRoom(room.Id, userInfo.UserId,
            async (dismissedRoomId, userIds) =>
            {
                // 通知所有成员房间解散
                var dismissedPacket = new RoomDismissedPacket(dismissedRoomId);
                foreach (var uid in userIds)
                {
                    await Server.GetConnectionManager().SendPacketToUserAsync(uid, dismissedPacket);
                }
            });
        // 广播成员变动
        var infoPacket = new RoomInfoPacket(room);
        foreach (var u in room.Users)
        {
            await Server.GetConnectionManager().SendPacketToUserAsync(u.Key, infoPacket);
        }
    }
} 