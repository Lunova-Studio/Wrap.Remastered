using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

public sealed class RoomJoinRequestNoticePacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomJoinRequestNoticePacketSerializer();

    public int RoomId { get; set; }
    public string ApplicantUserId { get; set; } = string.Empty;

    public RoomJoinRequestNoticePacket() { }

    public RoomJoinRequestNoticePacket(int roomId, string applicantUserId) {
        RoomId = roomId;
        ApplicantUserId = applicantUserId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomJoinRequestNoticePacket;
}