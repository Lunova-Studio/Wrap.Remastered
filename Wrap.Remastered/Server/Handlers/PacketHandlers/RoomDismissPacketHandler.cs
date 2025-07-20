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

public class RoomDismissPacketHandler : RoomPacketHandler
{
    public RoomDismissPacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var req = RoomDismissPacket.Serializer.Deserialize(packet.Data) as RoomDismissPacket;
        if (req == null) return;
        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null) return;
        var ownerConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        if (ownerConn == null || room.Owner.UserId != ownerConn.UserInfo!.UserId) return;
        var userIds = room.Users.Select(u => u.Key).ToList();
        RoomManager.RemoveRoom(room.Id);
        var dismissedPacket = new RoomDismissedPacket(room.Id);
        foreach (var uid in userIds)
        {
            await Server.GetConnectionManager().SendPacketToUserAsync(uid, dismissedPacket);
        }
    }
} 