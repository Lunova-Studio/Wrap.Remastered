using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class ProxyDataPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not ProxyDataPacket packet)
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();

        stream.WriteInt64(packet.SequenceId);
        stream.WriteString(packet.ConnectionId);
        stream.WriteInt32(packet.Data.Length);
        stream.WriteBytes(packet.Data);
        stream.WriteBool(packet.IsClientToServer);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        ProxyDataPacket packet = new();
        using MemoryStream stream = new(data);

        packet.SequenceId = stream.ReadInt64();
        packet.ConnectionId = stream.ReadString();

        int dataLength = stream.ReadInt32();

        packet.Data = stream.ReadBytes(dataLength);
        packet.IsClientToServer = stream.ReadBool();

        return packet;
    }
}