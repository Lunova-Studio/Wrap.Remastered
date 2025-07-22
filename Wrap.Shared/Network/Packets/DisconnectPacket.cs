using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public sealed class DisconnectPacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new DisconnectPacketSerializer();

    public string Reason { get; set; } = string.Empty;
    public DateTime DisconnectTime { get; set; } = DateTime.Now;

    public DisconnectPacket(string? reason) {
        Reason = reason == null ? string.Empty : reason;
        DisconnectTime = DateTime.Now;
    }

    public DisconnectPacket() { }

    public ISerializer<IPacket> GetSerializer() => Serializer;

    public void OnSerialize(ref byte[] data) { }

    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.DisconnectPacket;
}