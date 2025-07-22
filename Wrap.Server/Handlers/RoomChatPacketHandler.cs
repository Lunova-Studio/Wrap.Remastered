using DotNetty.Transport.Channels;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Remastered.Server.Managers;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public sealed class RoomChatPacketHandler : RoomPacketHandler {
    public RoomChatPacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        if (RoomChatPacket.Serializer.Deserialize(packet.Data) is not RoomChatPacket req)
            return;

        if (string.IsNullOrWhiteSpace(req.Message))
            return;

        if (req.Message.Length > 255)
            req.Message = req.Message[..255];

        var userConn = Server.GetConnectionManager()
            .GetAllUserConnections()
            .FirstOrDefault(c => c.Channel == channel);

        var userInfo = userConn?.UserInfo;
        if (userInfo == null)
            return;

        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null || !room.Users.ContainsKey(userInfo.UserId))
            return;

        var msgPacket = new RoomChatMessagePacket(room.Id,
            senderUserId: userInfo.UserId,
            senderDisplayName: userInfo.DisplayName,
            message: req.Message,
            timestamp: DateTime.UtcNow
        );

        foreach (var user in room.Users) {
            await Server.GetConnectionManager()
                .SendPacketToUserAsync(user.Key, msgPacket);
        }
    }
}
