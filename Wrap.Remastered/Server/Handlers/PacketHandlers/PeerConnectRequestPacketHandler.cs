using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Server.Managers;
using DotNetty.Transport.Channels;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ClientBound;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

/// <summary>
/// P2P连接请求包处理器
/// </summary>
public class PeerConnectRequestPacketHandler : BasePacketHandler
{
    public PeerConnectRequestPacketHandler(IWrapServer server) : base(server)
    {
    }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        try
        {
            // 反序列化P2P连接请求包
            var peerConnectRequestPacket = PeerConnectRequestPacket.Serializer.Deserialize(packet.Data) as PeerConnectRequestPacket;
            if (peerConnectRequestPacket == null)
            {
                Server.GetLoggingService().LogError("Packet", "P2P连接请求包反序列化失败", null, "通道: {0}", channel.RemoteAddress);
                return;
            }

            var connection = Server.GetConnectionManager().GetChannelConnection(channel);
            if (connection == null)
            {
                Server.GetLoggingService().LogError("Packet", "无法获取用户连接", null, "通道: {0}", channel.RemoteAddress);
                return;
            }

            var requesterUserId = connection.UserInfo!.UserId;
            var targetUserId = peerConnectRequestPacket.TargetUserId;

            // 检查目标用户是否在同一个房间
            var room = Server.GetRoomManager().GetUserRoom(requesterUserId);
            if (room == null)
            {
                // 用户不在房间中，无法建立P2P连接
                return;
            }

            // 检查目标用户是否在房间中
            if (!room.Users.ContainsKey(targetUserId))
            {
                // 目标用户不在房间中
                return;
            }

            // 检查是否为房主向其他用户发起连接
            if (room.OwnerUserId != requesterUserId)
            {
                // 只有房主可以发起P2P连接
                return;
            }

            // 处理P2P连接请求
            var success = Server.GetPeerManager().HandlePeerConnectRequest(requesterUserId, targetUserId);
            
            if (!success)
            {
                // 发送失败通知
                var failedPacket = new PeerConnectFailedNoticePacket(targetUserId, "目标用户不可用", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
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