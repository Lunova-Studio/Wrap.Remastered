using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Shared.Network.Serializers.Client;

public sealed class RoomChatMessagePacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not RoomChatMessagePacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.SenderUserId);
        stream.WriteString(p.SenderDisplayName);
        stream.WriteString(p.Message);
        stream.WriteInt64(p.Timestamp.ToBinary());
        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        RoomChatMessagePacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.SenderUserId = stream.ReadString();
        packet.SenderDisplayName = stream.ReadString();
        packet.Message = stream.ReadString();
        packet.Timestamp = DateTime.FromBinary(stream.ReadInt64());
        return packet;
    }
}