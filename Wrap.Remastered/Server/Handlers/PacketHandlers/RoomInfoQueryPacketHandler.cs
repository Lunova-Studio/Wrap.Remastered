using DotNetty.Transport.Channels;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Server.Managers;
using Wrap.Remastered.Interfaces;
using System.Linq;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public class RoomInfoQueryPacketHandler : RoomPacketHandler
{
    public RoomInfoQueryPacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var req = RoomInfoQueryPacket.Serializer.Deserialize(packet.Data) as RoomInfoQueryPacket;
        if (req == null) return;
        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null) return;
        var resultPacket = new RoomInfoQueryResultPacket(room.Id, room.Name, room.Owner.UserId, room.MaxUsers, room.Users.Count);
        var requester = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel)?.UserInfo;
        if (requester != null)
        {
            Server.GetConnectionManager().SendPacketToUserAsync(requester.UserId, resultPacket).Wait();
        }
    }
} 