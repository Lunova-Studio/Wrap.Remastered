using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

public enum ServerBoundPacketType
{
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
    KeepAliveResponsePacket
}
