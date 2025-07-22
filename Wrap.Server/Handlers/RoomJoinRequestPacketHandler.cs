using DotNetty.Transport.Channels;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Remastered.Server.Managers;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class RoomJoinRequestPacketHandler : RoomPacketHandler {
    public RoomJoinRequestPacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (RoomJoinRequestPacket.Serializer.Deserialize(packet.Data) is not RoomJoinRequestPacket req) 
            return;

        var userConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        var userInfo = userConn?.UserInfo;
        if (userInfo == null)
            return;

        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null)
            return;

        // 通知房主有用户申请加入
        var notice = new RoomJoinRequestNoticePacket(room.Id, userInfo.UserId);
        await Server.GetConnectionManager().SendPacketToUserAsync(room.Owner.UserId, notice);
    }
}