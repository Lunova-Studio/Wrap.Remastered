using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class PeerKeepAliveResponsePacketSerializer : ISerializer<IPacket> {
    public IPacket Deserialize(byte[] data) {
        PeerKeepAliveResponsePacket packet = new();

        using MemoryStream stream = new(data);

        packet.Value = stream.ReadInt32();

        return packet;
    }

    public byte[] Serialize(IPacket obj) {
        if (obj is not PeerKeepAliveResponsePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteInt32(packet.Value);

        return stream.ToArray();
    }
}