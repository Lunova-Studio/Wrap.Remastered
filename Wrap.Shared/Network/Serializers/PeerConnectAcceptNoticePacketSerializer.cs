using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class PeerConnectAcceptNoticePacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not PeerConnectAcceptNoticePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.AccepterUserId);
        stream.WriteString(packet.AccepterDisplayName);
        stream.WriteInt64(packet.Timestamp);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        PeerConnectAcceptNoticePacket packet = new();

        using MemoryStream stream = new(data);

        packet.AccepterUserId = stream.ReadString();
        packet.AccepterDisplayName = stream.ReadString();
        packet.Timestamp = stream.ReadInt64();

        return packet;
    }
}