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

public class RoomTransferOwnerPacketHandler : RoomPacketHandler
{
    public RoomTransferOwnerPacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var req = RoomTransferOwnerPacket.Serializer.Deserialize(packet.Data) as RoomTransferOwnerPacket;
        if (req == null) return;
        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null) return;
        var ownerConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        if (ownerConn == null || room.Owner.UserId != ownerConn.UserInfo.UserId) return;
        var newOwner = room.Users.FirstOrDefault(u => u.UserId == req.NewOwnerUserId);
        if (newOwner == null) return;
        room.Owner = newOwner;
        var ownerChangedPacket = new RoomOwnerChangedPacket(room.Id, newOwner.UserId);
        foreach (var u in room.Users)
        {
            await Server.GetConnectionManager().SendPacketToUserAsync(u.UserId, ownerChangedPacket);
        }
    }
} 