using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Shared.Network.Serializers.Client;

public sealed class ServerMessagePacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not ServerMessagePacket msgPacket) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteString(msgPacket.Message);
        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        ServerMessagePacket packet = new();
        using MemoryStream stream = new(data);
        packet.Message = stream.ReadString();
        return packet;
    }
}