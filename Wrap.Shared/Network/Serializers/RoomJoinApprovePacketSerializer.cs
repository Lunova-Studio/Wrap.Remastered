using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class RoomJoinApprovePacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not RoomJoinApprovePacket packet)
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();

        stream.WriteInt32(packet.RoomId);
        stream.WriteString(packet.UserId);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        using MemoryStream stream = new(data);
        var packet = new RoomJoinApprovePacket {
            RoomId = stream.ReadInt32(),
            UserId = stream.ReadString()
        };

        return packet;
    }
}
