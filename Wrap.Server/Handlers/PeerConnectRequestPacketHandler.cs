using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;
using Wrap.Shared.Network.Packets.Client;
using Wrap.Shared.Network.Packets.Server;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

/// <summary>
/// P2P连接请求包处理器
/// </summary>
public sealed class PeerConnectRequestPacketHandler : BasePacketHandler {
    public PeerConnectRequestPacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        try {
            if (PeerConnectRequestPacket.Serializer.Deserialize(packet.Data) is not PeerConnectRequestPacket req) {
                Server.Logger.LogError(new ArgumentNullException(nameof(req)),
                    "P2P 连接请求包反序列化失败, 通道: {RemoteAddress}", channel.RemoteAddress);

                return;
            }

            var connection = Server.GetConnectionManager().GetChannelConnection(channel);
            if (connection?.UserInfo == null) {
                Server.Logger.LogError(new ArgumentNullException(nameof(connection.UserInfo)),
                    "无法获取用户连接, 通道:{RemoteAddress}", channel.RemoteAddress);

                return;
            }

            var requesterUserId = connection.UserInfo.UserId;
            var targetUserId = req.TargetUserId;

            var room = Server.GetRoomManager().GetUserRoom(requesterUserId);
            if (room == null || !room.Users.ContainsKey(targetUserId))
                return;

            if (room.OwnerUserId != requesterUserId)
                return;

            var success = Server.GetPeerManager().HandlePeerConnectRequest(requesterUserId, targetUserId);
            if (!success) {
                var failedPacket = new PeerConnectFailedNoticePacket(
                    targetUserId,
                    "目标用户不可用",
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                );

                await SendPacketAsync(channel, failedPacket);
            }
        } catch (Exception ex) {
            OnError(channel, packet, ex);
        }
    }

    private static async Task SendPacketAsync(IChannel channel, IClientBoundPacket packet) {
        var serializer = packet.GetSerializer();
        var data = serializer.Serialize(packet);
        packet.OnSerialize(ref data);
        var packetData = new byte[4 + data.Length];
        BitConverter.GetBytes((int)packet.GetPacketType()).CopyTo(packetData, 0);
        data.CopyTo(packetData, 4);

        await channel.WriteAndFlushAsync(DotNetty.Buffers.Unpooled.WrappedBuffer(packetData));
    }
}
