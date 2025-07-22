using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class PeerConnectFailedPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not PeerConnectFailedPacket packet)
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();

        stream.WriteString(packet.TargetUserId);
        stream.WriteString(packet.Reason);
        stream.WriteInt64(packet.Timestamp);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        using MemoryStream stream = new(data);
        var packet = new PeerConnectFailedPacket {
            TargetUserId = stream.ReadString(),
            Reason = stream.ReadString(),
            Timestamp = stream.ReadInt64()
        };

        return packet;
    }
}
