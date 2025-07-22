using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets.Server;

namespace Wrap.Shared.Network.Serializers.Server;

public sealed class LoginPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not LoginPacket loginPacket) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(loginPacket.Name);
        stream.WriteString(loginPacket.DisplayName);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        LoginPacket packet = new();

        using MemoryStream stream = new(data);

        packet.Name = stream.ReadString();
        packet.DisplayName = stream.ReadString();

        return packet;
    }
}
