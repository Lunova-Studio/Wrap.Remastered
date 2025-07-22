using DotNetty.Transport.Channels;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Remastered.Server.Managers;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class RoomJoinApprovePacketHandler : RoomPacketHandler {
    public RoomJoinApprovePacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (RoomJoinApprovePacket.Serializer.Deserialize(packet.Data) is not RoomJoinApprovePacket req)
            return;

        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null)
            return;

        var user = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.UserInfo.UserId == req.UserId)?.UserInfo;
        if (user == null)
            return;

        // 只有房主能同意
        var ownerConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        if (ownerConn == null || room.Owner.UserId != ownerConn.UserInfo!.UserId)
            return;

        var success = RoomManager.AddUserToRoom(room.Id, user);
        // 通知申请者结果
        var result = new RoomJoinResultPacket(room.Id, success, success ? "加入房间成功" : "加入房间失败");
        await Server.GetConnectionManager().SendPacketToUserAsync(user.UserId, result);
        // 广播成员变动
        if (success) {
            var infoPacket = new RoomInfoPacket(room);
            foreach (var u in room.Users) {
                await Server.GetConnectionManager()
                    .SendPacketToUserAsync(u.Value.UserId, infoPacket);
            }

            // 房主会在收到房间信息更新后自动发起P2P连接
            // 这里不需要手动发起，避免重复连接
        }
    }
}