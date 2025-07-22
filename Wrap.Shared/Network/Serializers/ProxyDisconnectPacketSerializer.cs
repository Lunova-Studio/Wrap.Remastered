using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class ProxyDisconnectPacketSerializer : ISerializer<IPacket> {
    public IPacket Deserialize(byte[] data) {
        ProxyDisconnectPacket packet = new();

        using MemoryStream stream = new(data);

        packet.ConnectionId = stream.ReadString();
        packet.Reason = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj) {
        if (obj is not ProxyDisconnectPacket packet) 
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();

        stream.WriteString(packet.ConnectionId);
        stream.WriteString(packet.Reason);

        return stream.ToArray();
    }
}