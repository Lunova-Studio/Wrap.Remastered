using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class RoomJoinResultPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not RoomJoinResultPacket p)
            throw new ArgumentException();

        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteInt32(p.Success ? 1 : 0);
        stream.WriteString(p.Message);
        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        RoomJoinResultPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.Success = stream.ReadInt32() != 0;
        packet.Message = stream.ReadString();
        return packet;
    }
}