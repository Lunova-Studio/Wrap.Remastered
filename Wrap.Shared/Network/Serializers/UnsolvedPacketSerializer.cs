using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class UnsolvedPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not UnsolvedPacket unsolvedPacket) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteInt32(unsolvedPacket.PacketType);
        stream.WriteVarInt(unsolvedPacket.Data.Length);
        stream.Write(unsolvedPacket.Data);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        UnsolvedPacket packet = new();

        using MemoryStream stream = new(data);

        packet.PacketType = stream.ReadInt32();
        int len = stream.ReadVarInt();

        // 分配内存并读取数据
        packet.Data = new byte[len];
        stream.Read(packet.Data, 0, len);

        return packet;
    }
}
