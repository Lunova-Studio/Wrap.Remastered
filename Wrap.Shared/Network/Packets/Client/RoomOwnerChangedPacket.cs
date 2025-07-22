using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

public sealed class RoomOwnerChangedPacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomOwnerChangedPacketSerializer();
    public int RoomId { get; set; }
    public string NewOwnerUserId { get; set; } = string.Empty;

    public RoomOwnerChangedPacket() { }

    public RoomOwnerChangedPacket(int roomId, string newOwnerUserId) {
        RoomId = roomId;
        NewOwnerUserId = newOwnerUserId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomOwnerChangedPacket;
}