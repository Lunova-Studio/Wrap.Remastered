using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Shared.Network.Serializers.Client;

public sealed class DisconnectPacketSerializer : ISerializer<IPacket> {
    public IPacket Deserialize(byte[] data) {
        DisconnectPacket packet = new();
        using MemoryStream stream = new(data);
        packet.Reason = stream.ReadString();
        packet.DisconnectTime = DateTime.FromBinary(stream.ReadInt64());
        return packet;
    }

    public byte[] Serialize(IPacket obj) {
        if (obj is not DisconnectPacket disconnectPacket)
            throw new ArgumentException("Invalid packet type for serialization.", nameof(obj));

        using MemoryStream stream = new();
        stream.WriteString(disconnectPacket.Reason);
        stream.WriteInt64(disconnectPacket.DisconnectTime.ToBinary());
        return stream.ToArray();
    }
}