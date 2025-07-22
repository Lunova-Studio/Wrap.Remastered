using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Server;

namespace Wrap.Shared.Network.Serializers;

public sealed class PeerConnectRejectPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not PeerConnectRejectPacket packet)
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();

        stream.WriteString(packet.RequesterUserId);
        stream.WriteString(packet.Reason);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        using MemoryStream stream = new(data);
        var packet = new PeerConnectRejectPacket {
            RequesterUserId = stream.ReadString(),
            Reason = stream.ReadString()
        };

        return packet;
    }
}
