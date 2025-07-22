using Wrap.Shared.Enums;
using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Shared.Network.Serializers.Client;

public sealed class KeepAlivePacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not KeepAlivePacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.Value);
        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        KeepAlivePacket packet = new();
        using MemoryStream stream = new(data);
        packet.Value = stream.ReadInt32();
        return packet;
    }
}