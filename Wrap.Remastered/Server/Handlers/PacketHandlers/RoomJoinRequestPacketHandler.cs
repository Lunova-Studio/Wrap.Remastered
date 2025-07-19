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

public class RoomJoinRequestPacketHandler : RoomPacketHandler
{
    public RoomJoinRequestPacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var req = RoomJoinRequestPacket.Serializer.Deserialize(packet.Data) as RoomJoinRequestPacket;
        if (req == null) return;
        var userConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        var userInfo = userConn?.UserInfo;
        if (userInfo == null) return;
        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null) return;
        // 通知房主有用户申请加入
        var notice = new RoomJoinRequestNoticePacket(room.Id, userInfo.UserId);
        await Server.GetConnectionManager().SendPacketToUserAsync(room.Owner.UserId, notice);
    }
} 