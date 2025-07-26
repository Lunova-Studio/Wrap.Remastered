namespace Wrap.Shared.Enums;

public enum ClientBoundPacketType {
    LoginSucceedPacket,
    LoginFailedPacket,
    DisconnectPacket,
    RoomInfoPacket,
    UserInfoResultPacket,
    RoomJoinRequestNoticePacket,
    RoomJoinResultPacket,
    RoomOwnerChangedPacket,
    RoomDismissedPacket,
    RoomInfoQueryResultPacket,
    RoomChatMessagePacket,
    KeepAlivePacket,
    PeerConnectRequestNoticePacket,
    PeerConnectAcceptNoticePacket,
    PeerConnectRejectNoticePacket,
    PeerIPInfoPacket,
    PeerConnectFailedNoticePacket,
    PluginMessage,
    ServerMessagePacket,
    PingInfoPacket
}