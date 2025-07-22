using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class PeerConnectRejectNoticePacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not PeerConnectRejectNoticePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.RejecterUserId);
        stream.WriteString(packet.RejecterDisplayName);
        stream.WriteString(packet.Reason);
        stream.WriteInt64(packet.Timestamp);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        PeerConnectRejectNoticePacket packet = new();

        using MemoryStream stream = new(data);

        packet.RejecterUserId = stream.ReadString();
        packet.RejecterDisplayName = stream.ReadString();
        packet.Reason = stream.ReadString();
        packet.Timestamp = stream.ReadInt64();

        return packet;
    }
}