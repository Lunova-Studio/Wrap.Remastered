using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class PeerIPInfoPacketSerializer : ISerializer<IPacket> {
    public IPacket Deserialize(byte[] data) {
        PeerIPInfoPacket packet = new();

        using MemoryStream stream = new(data);

        packet.TargetUserId = stream.ReadString();
        packet.TargetIPAddress = stream.ReadBytes(4);
        packet.TargetPort = stream.ReadInt32();

        return packet;
    }

    public byte[] Serialize(IPacket obj) {
        if (obj is not PeerIPInfoPacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.TargetUserId);
        stream.WriteBytes(packet.TargetIPAddress);
        stream.WriteInt32(packet.TargetPort);

        return stream.ToArray();
    }
}