using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class PeerConnectFailedNoticePacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not PeerConnectFailedNoticePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.TargetUserId);
        stream.WriteString(packet.Reason);
        stream.WriteInt64(packet.Timestamp);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        PeerConnectFailedNoticePacket packet = new();

        using MemoryStream stream = new(data);

        packet.TargetUserId = stream.ReadString();
        packet.Reason = stream.ReadString();
        packet.Timestamp = stream.ReadInt64();

        return packet;
    }
}