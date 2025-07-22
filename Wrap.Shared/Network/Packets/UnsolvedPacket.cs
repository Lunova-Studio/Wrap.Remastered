using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public sealed class UnsolvedPacket : IPacket {
    public static ISerializer<IPacket> Serializer { get; } = new UnsolvedPacketSerializer();

    public int PacketType { get; set; }
    public byte[] Data { get; set; } = [];
    public ISerializer<IPacket> GetSerializer() {
        return Serializer;
    }

    public UnsolvedPacket() { }

    public UnsolvedPacket(int packetType, byte[] data) {
        PacketType = packetType;
        Data = data;
    }

    public void OnSerialize(ref byte[] data) { }
}