using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class PeerConnectSuccessPacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not PeerConnectSuccessPacket packet)
            throw new ArgumentException(nameof(packet));

        using MemoryStream stream = new();
        stream.WriteString(packet.TargetUserId);
        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        using MemoryStream stream = new(data);
        var packet = new PeerConnectSuccessPacket {
            TargetUserId = stream.ReadString()
        };

        return packet;
    }
}
