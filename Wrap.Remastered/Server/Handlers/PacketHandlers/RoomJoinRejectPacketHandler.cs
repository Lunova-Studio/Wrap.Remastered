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

public class RoomJoinRejectPacketHandler : RoomPacketHandler
{
    public RoomJoinRejectPacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var req = RoomJoinRejectPacket.Serializer.Deserialize(packet.Data) as RoomJoinRejectPacket;
        if (req == null) return;
        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null) return;
        var ownerConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        if (ownerConn == null || room.Owner.UserId != ownerConn.UserInfo.UserId) return;
        await Server.GetConnectionManager().SendPacketToUserAsync(req.UserId, new RoomJoinResultPacket(room.Id, false, "房主拒绝了你的加入请求"));
    }
} 