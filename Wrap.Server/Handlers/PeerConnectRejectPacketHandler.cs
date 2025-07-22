using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

/// <summary>
/// P2P连接拒绝包处理器
/// </summary>
public sealed class PeerConnectRejectPacketHandler : BasePacketHandler {
    public PeerConnectRejectPacketHandler(IServerCoordinator server) : base(server) {}

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        try {
            if (PeerConnectRejectPacket.Serializer.Deserialize(packet.Data) is not PeerConnectRejectPacket rejectPacket) {
                Server.Logger.LogError(new ArgumentNullException(nameof(rejectPacket)),
                    "P2P连接拒绝包反序列化失败, 通道: {RemoteAddress}", channel.RemoteAddress);

                return;
            }

            var connection = Server.GetConnectionManager().GetChannelConnection(channel);
            if (connection == null) {
                Server.Logger.LogError(new ArgumentNullException(nameof(connection)),
                    "无法获取用户连接, 通道: {RemoteAddress}", channel.RemoteAddress);

                return;
            }

            var rejecterUserId = connection.UserInfo!.UserId;
            var requesterUserId = rejectPacket.RequesterUserId;
            var reason = rejectPacket.Reason;

            // 检查用户是否在同一个房间
            var room = Server.GetRoomManager().GetUserRoom(rejecterUserId);
            if (room == null) {
                // 用户不在房间中，无法建立P2P连接
                return;
            }

            // 检查请求者是否在房间中
            if (!room.Users.ContainsKey(requesterUserId)) {
                // 请求者不在房间中
                return;
            }

            // 检查是否为普通成员拒绝房主的连接
            if (room.OwnerUserId != requesterUserId) {
                // 只有房主可以发起P2P连接
                return;
            }

            // 处理P2P连接拒绝
            var success = Server.GetPeerManager().HandlePeerConnectReject(rejecterUserId, requesterUserId, reason);

            if (!success) {
                // 发送失败通知
                var failedPacket = new PeerConnectFailedNoticePacket(requesterUserId, "连接请求已过期", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                await SendPacketAsync(channel, failedPacket);
            }
        } catch (Exception ex) {
            OnError(channel, packet, ex);
        }
    }

    private async Task SendPacketAsync(IChannel channel, IClientBoundPacket packet) {
        var serializer = packet.GetSerializer();
        var data = serializer.Serialize(packet);
        packet.OnSerialize(ref data);
        var packetData = new byte[4 + data.Length];
        BitConverter.GetBytes((int)packet.GetPacketType()).CopyTo(packetData, 0);
        data.CopyTo(packetData, 4);
        var buffer = DotNetty.Buffers.Unpooled.WrappedBuffer(packetData);
        await channel.WriteAndFlushAsync(buffer);
    }
}