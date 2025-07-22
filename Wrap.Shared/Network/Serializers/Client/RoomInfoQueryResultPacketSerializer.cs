using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Shared.Network.Serializers.Client;

public sealed class RoomInfoQueryResultPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not RoomInfoQueryResultPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.RoomName);
        stream.WriteString(p.OwnerUserId);
        stream.WriteInt32(p.MaxUsers);
        stream.WriteInt32(p.UserCount);
        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        RoomInfoQueryResultPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.RoomName = stream.ReadString();
        packet.OwnerUserId = stream.ReadString();
        packet.MaxUsers = stream.ReadInt32();
        packet.UserCount = stream.ReadInt32();
        return packet;
    }
}
