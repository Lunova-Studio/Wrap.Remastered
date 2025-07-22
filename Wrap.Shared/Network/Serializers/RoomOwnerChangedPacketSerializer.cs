using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class RoomOwnerChangedPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not RoomOwnerChangedPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.NewOwnerUserId);
        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        RoomOwnerChangedPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.NewOwnerUserId = stream.ReadString();
        return packet;
    }
}