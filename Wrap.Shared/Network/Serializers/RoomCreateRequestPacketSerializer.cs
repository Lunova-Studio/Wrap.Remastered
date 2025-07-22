using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class RoomCreateRequestPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not RoomCreateRequestPacket p)
            throw new ArgumentException(nameof(p));

        using MemoryStream stream = new();

        stream.WriteString(p.RoomName);
        stream.WriteInt32(p.MaxUsers);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        RoomCreateRequestPacket packet = new();
        using MemoryStream stream = new(data);

        packet.RoomName = stream.ReadString();
        packet.MaxUsers = stream.ReadInt32();

        return packet;
    }
}