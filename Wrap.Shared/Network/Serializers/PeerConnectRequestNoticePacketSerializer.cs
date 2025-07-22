using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class PeerConnectRequestNoticePacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not PeerConnectRequestNoticePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.RequesterUserId);
        stream.WriteString(packet.RequesterDisplayName);
        stream.WriteInt64(packet.Timestamp);

        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        PeerConnectRequestNoticePacket packet = new();

        using MemoryStream stream = new(data);

        packet.RequesterUserId = stream.ReadString();
        packet.RequesterDisplayName = stream.ReadString();
        packet.Timestamp = stream.ReadInt64();

        return packet;
    }
}