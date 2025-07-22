using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class ProxyConnectPacketSerializer : ISerializer<IPacket> {
    public IPacket Deserialize(byte[] data) {
        ProxyConnectPacket packet = new();

        using MemoryStream stream = new(data);
        packet.ConnectionId = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj) {
        if (obj is not ProxyConnectPacket packet)
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();

        stream.WriteString(packet.ConnectionId);
        return stream.ToArray();
    }
}