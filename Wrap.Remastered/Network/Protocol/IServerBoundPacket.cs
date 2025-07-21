using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ServerBound;

namespace Wrap.Remastered.Network.Protocol;

public interface IServerBoundPacket : IPacket
{
    public static Dictionary<ServerBoundPacketType, ISerializer<IPacket>> Serializers = new()
    {
        { ServerBoundPacketType.LoginPacket, new LoginPacketSerializer() },
        { ServerBoundPacketType.RoomCreateRequestPacket, new RoomCreateRequestPacketSerializer() },
        { ServerBoundPacketType.UserInfoQueryPacket, new UserInfoQueryPacketSerializer() },
        { ServerBoundPacketType.RoomJoinRequestPacket, new RoomJoinRequestPacketSerializer() },
        { ServerBoundPacketType.RoomJoinApprovePacket, new RoomJoinApprovePacketSerializer() },
        { ServerBoundPacketType.RoomLeavePacket, new RoomLeavePacketSerializer() },
        { ServerBoundPacketType.RoomInfoQueryPacket, new RoomInfoQueryPacketSerializer() },
        { ServerBoundPacketType.RoomKickPacket, new RoomKickPacketSerializer() },
        { ServerBoundPacketType.RoomJoinRejectPacket, new RoomJoinRejectPacketSerializer() },
        { ServerBoundPacketType.RoomTransferOwnerPacket, new RoomTransferOwnerPacketSerializer() },
        { ServerBoundPacketType.RoomDismissPacket, new RoomDismissPacketSerializer() },
        { ServerBoundPacketType.RoomChatPacket, new RoomChatPacketSerializer() },
        { ServerBoundPacketType.KeepAliveResponsePacket, new KeepAliveResponsePacketSerializer() },
        { ServerBoundPacketType.PeerConnectRequestPacket, new PeerConnectRequestPacketSerializer() },
        { ServerBoundPacketType.PeerConnectAcceptPacket, new PeerConnectAcceptPacketSerializer() },
        { ServerBoundPacketType.PeerConnectRejectPacket, new PeerConnectRejectPacketSerializer() },
        { ServerBoundPacketType.PeerConnectFailedPacket, new PeerConnectFailedPacketSerializer() },
        { ServerBoundPacketType.PeerConnectSuccessPacket, new PeerConnectSuccessPacketSerializer() },
        { ServerBoundPacketType.PluginMessage, new PluginMessagePacketSerializer() }
    };

    ServerBoundPacketType GetPacketType();
}
