using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Shared.Network.Serializers.Client;

public sealed class LoginFailedPacketSerializer : ISerializer<IPacket> {
    public IPacket Deserialize(byte[] data) {
        LoginFailedPacket packet = new();

        using MemoryStream stream = new(data);

        packet.ErrorCode = stream.ReadInt32();
        packet.ErrorMessage = stream.ReadString();
        packet.FailTime = DateTime.FromBinary(stream.ReadInt64());

        return packet;
    }

    public byte[] Serialize(IPacket obj) {
        if (obj is not LoginFailedPacket loginFailedPacket) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteInt32(loginFailedPacket.ErrorCode);
        stream.WriteString(loginFailedPacket.ErrorMessage);
        stream.WriteInt64(loginFailedPacket.FailTime.ToBinary());

        return stream.ToArray();
    }
}
