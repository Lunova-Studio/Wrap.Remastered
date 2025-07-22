using DotNetty.Transport.Channels;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Remastered.Server.Managers;
using Wrap.Shared.Network.Packets;
using Wrap.Shared.Network.Packets.Client;
using Wrap.Shared.Network.Packets.Server;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class RoomInfoQueryPacketHandler : RoomPacketHandler {
    public RoomInfoQueryPacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (RoomInfoQueryPacket.Serializer.Deserialize(packet.Data) is not RoomInfoQueryPacket req)
            return;

        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null)
            return;

        var resultPacket = new RoomInfoQueryResultPacket(room.Id, room.Name, room.Owner.UserId, room.MaxUsers, room.Users.Count);
        var requester = Server.GetConnectionManager()
            .GetAllUserConnections()
            .FirstOrDefault(c => c.Channel == channel)?.UserInfo;

        if (requester != null)
            await Server.GetConnectionManager().SendPacketToUserAsync(requester.UserId, resultPacket);
    }
}