using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class PeerKeepAlivePacketSerializer : ISerializer<IPacket> {
    public IPacket Deserialize(byte[] data) {
        PeerKeepAlivePacket packet = new();
        using MemoryStream stream = new(data);

        packet.Value = stream.ReadInt32();
        return packet;
    }

    public byte[] Serialize(IPacket obj) {
        if (obj is not PeerKeepAlivePacket packet)
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();
        stream.WriteInt32(packet.Value);
        return stream.ToArray();
    }
}