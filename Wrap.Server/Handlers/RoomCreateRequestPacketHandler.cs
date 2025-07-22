using DotNetty.Transport.Channels;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Remastered.Server.Managers;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class RoomCreateRequestPacketHandler : RoomPacketHandler {
    public RoomCreateRequestPacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (RoomCreateRequestPacket.Serializer.Deserialize(packet.Data) is not RoomCreateRequestPacket req)
            return;

        var userConn = Server.GetConnectionManager()
            .GetAllUserConnections()
            .FirstOrDefault(c => c.Channel == channel);

        var userInfo = userConn?.UserInfo;
        if (userInfo == null) 
            return;

        var room = RoomManager.CreateRoom(req.RoomName, userInfo, req.MaxUsers);
        // 通知房主
        var infoPacket = new RoomInfoPacket(room);
        await Server.GetConnectionManager().SendPacketToUserAsync(userInfo.UserId, infoPacket);
        // 广播成员变动
        await BroadcastRoomMembersAsync(room);
    }

    private async Task BroadcastRoomMembersAsync(Room room) {
        var infoPacket = new RoomInfoPacket(room);
        foreach (var user in room.Users) {
            await Server.GetConnectionManager().SendPacketToUserAsync(user.Key, infoPacket);
        }
    }

    private static DotNetty.Buffers.IByteBuffer PacketToBuffer(IClientBoundPacket packet) {
        var serializer = packet.GetSerializer();
        var data = serializer.Serialize(packet);
        var packetData = new byte[4 + data.Length];
        BitConverter.GetBytes((int)packet.GetPacketType()).CopyTo(packetData, 0);
        data.CopyTo(packetData, 4);
        return DotNetty.Buffers.Unpooled.WrappedBuffer(packetData);
    }
}