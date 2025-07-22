using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class LoginSucceedPacketSerializer : ISerializer<IPacket> {
    public IPacket Deserialize(byte[] data) {
        LoginSucceedPacket packet = new();

        using MemoryStream stream = new(data);

        packet.UserId = stream.ReadString();
        packet.Name = stream.ReadString();
        packet.DisplayName = stream.ReadString();
        packet.LoginTime = DateTime.FromBinary(stream.ReadInt64());
        packet.IPAddress = stream.ReadBytes(4);
        packet.Port = stream.ReadInt32();

        return packet;
    }

    public byte[] Serialize(IPacket obj) {
        if (obj is not LoginSucceedPacket loginSucceedPacket) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(loginSucceedPacket.UserId);
        stream.WriteString(loginSucceedPacket.Name);
        stream.WriteString(loginSucceedPacket.DisplayName);
        stream.WriteInt64(loginSucceedPacket.LoginTime.ToBinary());
        stream.WriteBytes(loginSucceedPacket.IPAddress);
        stream.WriteInt32(loginSucceedPacket.Port);

        return stream.ToArray();
    }
}