using DotNetty.Transport.Channels;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Network.Packets;
using Wrap.Shared.Network.Packets.Client;
using Wrap.Shared.Network.Packets.Server;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class UserInfoQueryPacketHandler : RoomPacketHandler {
    public UserInfoQueryPacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (UserInfoQueryPacket.Serializer.Deserialize(packet.Data) is not UserInfoQueryPacket req)
            return;

        var userConn = Server.GetConnectionManager()
            .GetAllUserConnections()
            .FirstOrDefault(c => c.UserInfo.UserId == req.UserId);

        var userInfo = userConn?.UserInfo;
        if (userInfo == null)
            return;

        var resultPacket = new UserInfoResultPacket(userInfo);

        var requester = Server.GetConnectionManager()
            .GetAllUserConnections()
            .FirstOrDefault(c => c.Channel == channel)?.UserInfo;

        if (requester != null) {
            await Server.GetConnectionManager()
                .SendPacketToUserAsync(requester.UserId, resultPacket);
        }
    }
}