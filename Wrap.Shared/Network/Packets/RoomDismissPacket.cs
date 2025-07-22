using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public sealed class RoomDismissPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomDismissPacketSerializer();

    public int RoomId { get; set; }

    public RoomDismissPacket() { }

    public RoomDismissPacket(int roomId) {
        RoomId = roomId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomDismissPacket;
}