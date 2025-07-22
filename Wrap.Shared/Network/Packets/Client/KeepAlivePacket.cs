using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

public sealed class KeepAlivePacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new KeepAlivePacketSerializer();

    public int Value { get; set; }

    public KeepAlivePacket() { }

    public KeepAlivePacket(int value) {
        Value = value;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.KeepAlivePacket;
}