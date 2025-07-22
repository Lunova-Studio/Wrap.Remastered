namespace Wrap.Shared.Enums;

public enum ServerBoundPacketType {
    LoginPacket,
    RoomCreateRequestPacket,
    UserInfoQueryPacket,
    RoomJoinRequestPacket,
    RoomJoinApprovePacket,
    RoomLeavePacket,
    RoomInfoQueryPacket,
    RoomKickPacket,
    RoomJoinRejectPacket,
    RoomTransferOwnerPacket,
    RoomDismissPacket,
    RoomChatPacket,
    KeepAliveResponsePacket,
    PeerConnectRequestPacket,
    PeerConnectAcceptPacket,
    PeerConnectRejectPacket,
    PeerConnectFailedPacket,
    PeerConnectSuccessPacket,
    PluginMessage
}