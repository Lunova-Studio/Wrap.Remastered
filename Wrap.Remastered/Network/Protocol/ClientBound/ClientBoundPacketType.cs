namespace Wrap.Remastered.Network.Protocol.ClientBound;

public enum ClientBoundPacketType
{
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
    PluginMessage
}
