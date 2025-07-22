using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class RoomJoinRequestPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not RoomJoinRequestPacket packet)
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();
        stream.WriteInt32(packet.RoomId);
        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        using MemoryStream stream = new(data);
        var packet = new RoomJoinRequestPacket {
            RoomId = stream.ReadInt32()
        };
        return packet;
    }
}