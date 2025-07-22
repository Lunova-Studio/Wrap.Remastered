using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public sealed class RoomDismissedPacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomDismissedPacketSerializer();

    public int RoomId { get; set; }

    public RoomDismissedPacket() { }

    public RoomDismissedPacket(int roomId) {
        RoomId = roomId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomDismissedPacket;
}