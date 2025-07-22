using Wrap.Shared.Enums;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Interfaces;

public interface IClientBoundPacket : IPacket {
    public static Dictionary<ClientBoundPacketType, ISerializer<IPacket>> Serializers = new() {
        { ClientBoundPacketType.LoginSucceedPacket, new LoginSucceedPacketSerializer() },
        { ClientBoundPacketType.LoginFailedPacket, new LoginFailedPacketSerializer() },
        { ClientBoundPacketType.DisconnectPacket, new DisconnectPacketSerializer() },
        { ClientBoundPacketType.RoomInfoPacket, new RoomInfoPacketSerializer() },
        { ClientBoundPacketType.UserInfoResultPacket, new UserInfoResultPacketSerializer() },
        { ClientBoundPacketType.RoomJoinRequestNoticePacket, new RoomJoinRequestNoticePacketSerializer() },
        { ClientBoundPacketType.RoomJoinResultPacket, new RoomJoinResultPacketSerializer() },
        { ClientBoundPacketType.RoomOwnerChangedPacket, new RoomOwnerChangedPacketSerializer() },
        { ClientBoundPacketType.RoomDismissedPacket, new RoomDismissedPacketSerializer() },
        { ClientBoundPacketType.RoomInfoQueryResultPacket, new RoomInfoQueryResultPacketSerializer() },
        { ClientBoundPacketType.RoomChatMessagePacket, new RoomChatMessagePacketSerializer() },
        { ClientBoundPacketType.KeepAlivePacket, new KeepAlivePacketSerializer() },
        { ClientBoundPacketType.PeerConnectRequestNoticePacket, new PeerConnectRequestNoticePacketSerializer() },
        { ClientBoundPacketType.PeerConnectAcceptNoticePacket, new PeerConnectAcceptNoticePacketSerializer() },
        { ClientBoundPacketType.PeerConnectRejectNoticePacket, new PeerConnectRejectNoticePacketSerializer() },
        { ClientBoundPacketType.PeerIPInfoPacket, new PeerIPInfoPacketSerializer() },
        { ClientBoundPacketType.PeerConnectFailedNoticePacket, new PeerConnectFailedNoticePacketSerializer() },
        { ClientBoundPacketType.PluginMessage, new PluginMessagePacketSerializer() },
        { ClientBoundPacketType.ServerMessagePacket, new ServerMessagePacketSerializer() }
    };

    ClientBoundPacketType GetPacketType();
}