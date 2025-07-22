using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class ProxyResponsePacketSerializer : ISerializer<IPacket> {
    public IPacket Deserialize(byte[] data) {
        ProxyResponsePacket packet = new();

        using MemoryStream stream = new(data);

        packet.ConnectionId = stream.ReadString();
        packet.Success = stream.ReadBool();
        packet.ErrorMessage = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj) {
        if (obj is not ProxyResponsePacket packet)
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();

        stream.WriteString(packet.ConnectionId);
        stream.WriteBool(packet.Success);
        stream.WriteString(packet.ErrorMessage);

        return stream.ToArray();
    }
}