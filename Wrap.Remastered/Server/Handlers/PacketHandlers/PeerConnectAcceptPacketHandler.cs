using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Server.Managers;
using DotNetty.Transport.Channels;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ClientBound;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

/// <summary>
/// P2P连接接受包处理器
/// </summary>
public class PeerConnectAcceptPacketHandler : BasePacketHandler
{
    public PeerConnectAcceptPacketHandler(IWrapServer server) : base(server)
    {
    }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        try
        {
            // 反序列化P2P连接接受包
            var peerConnectAcceptPacket = PeerConnectAcceptPacket.Serializer.Deserialize(packet.Data) as PeerConnectAcceptPacket;
            if (peerConnectAcceptPacket == null)
            {
                Server.GetLoggingService().LogError("Packet", "P2P连接接受包反序列化失败", null, "通道: {0}", channel.RemoteAddress);
                return;
            }

            var connection = Server.GetConnectionManager().GetChannelConnection(channel);
            if (connection == null)
            {
                Server.GetLoggingService().LogError("Packet", "无法获取用户连接", null, "通道: {0}", channel.RemoteAddress);
                return;
            }

            var accepterUserId = connection.UserInfo!.UserId;
            var requesterUserId = peerConnectAcceptPacket.RequesterUserId;

            // 检查用户是否在同一个房间
            var room = Server.GetRoomManager().GetUserRoom(accepterUserId);
            if (room == null)
            {
                // 用户不在房间中，无法建立P2P连接
                return;
            }

            // 检查请求者是否在房间中
            if (!room.Users.ContainsKey(requesterUserId))
            {
                // 请求者不在房间中
                return;
            }

            // 检查是否为普通成员接受房主的连接
            if (room.OwnerUserId != requesterUserId)
            {
                // 只有房主可以发起P2P连接
                return;
            }

            // 处理P2P连接接受
            var success = Server.GetPeerManager().HandlePeerConnectAccept(accepterUserId, requesterUserId);
            
            if (!success)
            {
                // 发送失败通知
                var failedPacket = new PeerConnectFailedNoticePacket(requesterUserId, "连接请求已过期", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                await SendPacketAsync(channel, failedPacket);
            }
        }
        catch (Exception ex)
        {
            OnError(channel, packet, ex);
        }
    }

    private async Task SendPacketAsync(IChannel channel, IClientBoundPacket packet)
    {
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