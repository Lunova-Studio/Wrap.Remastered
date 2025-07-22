using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Server;

namespace Wrap.Shared.Network.Serializers.Server;


public sealed class RoomChatPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not RoomChatPacket packet)
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();

        stream.WriteInt32(packet.RoomId);
        stream.WriteString(packet.Message);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        using MemoryStream stream = new(data);
        var packet = new RoomChatPacket {
            RoomId = stream.ReadInt32(),
            Message = stream.ReadString()
        };

        return packet;
    }
}
