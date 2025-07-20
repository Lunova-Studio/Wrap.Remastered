using DotNetty.Transport.Channels;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Server.Managers;
using Wrap.Remastered.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public class RoomChatPacketHandler : RoomPacketHandler
{
    public RoomChatPacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var req = RoomChatPacket.Serializer.Deserialize(packet.Data) as RoomChatPacket;
        if (req == null) return;
        if (string.IsNullOrWhiteSpace(req.Message)) return;
        if (req.Message.Length > 255) req.Message = req.Message.Substring(0, 255);
        var userConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        var userInfo = userConn?.UserInfo;
        if (userInfo == null) return;
        var room = RoomManager.GetRoom(req.RoomId);
        if (room == null) return;
        if (!room.Users.Any(u => u.Key == userInfo.UserId)) return; // 只允许房间成员发言
        var msgPacket = new RoomChatMessagePacket(room.Id, userInfo.UserId, userInfo.DisplayName, req.Message, DateTime.UtcNow);
        foreach (var u in room.Users)
        {
            await Server.GetConnectionManager().SendPacketToUserAsync(u.Key, msgPacket);
        }
    }
} 