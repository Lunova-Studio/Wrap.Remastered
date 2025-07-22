using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Shared.Network.Serializers.Client;

public sealed class RoomJoinRequestNoticePacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not RoomJoinRequestNoticePacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.ApplicantUserId);
        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        RoomJoinRequestNoticePacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.ApplicantUserId = stream.ReadString();
        return packet;
    }
}