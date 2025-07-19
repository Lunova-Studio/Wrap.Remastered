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

public class UserInfoQueryPacketHandler : RoomPacketHandler
{
    public UserInfoQueryPacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var req = UserInfoQueryPacket.Serializer.Deserialize(packet.Data) as UserInfoQueryPacket;
        if (req == null) return;
        var user = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.UserInfo.UserId == req.UserId)?.UserInfo;
        if (user == null) return;
        var resultPacket = new UserInfoResultPacket(user);
        var requester = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel)?.UserInfo;
        if (requester != null)
        {
            await Server.GetConnectionManager().SendPacketToUserAsync(requester.UserId, resultPacket);
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