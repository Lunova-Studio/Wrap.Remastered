using DotNetty.Transport.Channels;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Server.Managers;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public class RoomCreateRequestPacketHandler : RoomPacketHandler
{
    public RoomCreateRequestPacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var req = RoomCreateRequestPacket.Serializer.Deserialize(packet.Data) as RoomCreateRequestPacket;
        if (req == null) return;
        var userConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        var userInfo = userConn?.UserInfo;
        if (userInfo == null) return;
        var room = RoomManager.CreateRoom(req.RoomName, userInfo, req.MaxUsers);
        // 通知房主
        var infoPacket = new RoomInfoPacket(room);
        await Server.GetConnectionManager().SendPacketToUserAsync(userInfo.UserId, infoPacket);
        // 广播成员变动
        await BroadcastRoomMembers(room);
    }

    private async Task BroadcastRoomMembers(Room room)
    {
        var infoPacket = new RoomInfoPacket(room);
        foreach (var user in room.Users)
        {
            await Server.GetConnectionManager().SendPacketToUserAsync(user.UserId, infoPacket);
        }
    }

    private static DotNetty.Buffers.IByteBuffer PacketToBuffer(IClientBoundPacket packet)
    {
        var serializer = packet.GetSerializer();
        var data = serializer.Serialize(packet);
        var packetData = new byte[4 + data.Length];
        BitConverter.GetBytes((int)packet.GetPacketType()).CopyTo(packetData, 0);
        data.CopyTo(packetData, 4);
        return DotNetty.Buffers.Unpooled.WrappedBuffer(packetData);
    }
}