using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Server;

namespace Wrap.Shared.Network.Packets.Server;

public sealed class KeepAliveResponsePacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new KeepAliveResponsePacketSerializer();

    public int Value { get; set; }

    public KeepAliveResponsePacket() { }
    public KeepAliveResponsePacket(int value) {
        Value = value;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.KeepAliveResponsePacket;
}