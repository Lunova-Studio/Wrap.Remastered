using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Server;

namespace Wrap.Shared.Network.Serializers.Server;

public sealed class RoomJoinRejectPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not RoomJoinRejectPacket packet)
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();

        stream.WriteInt32(packet.RoomId);
        stream.WriteString(packet.UserId);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        using MemoryStream stream = new(data);
        var packet = new RoomJoinRejectPacket {
            RoomId = stream.ReadInt32(),
            UserId = stream.ReadString()
        };

        return packet;
    }
}