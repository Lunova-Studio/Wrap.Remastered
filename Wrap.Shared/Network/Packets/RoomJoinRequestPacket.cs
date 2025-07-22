using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public sealed class RoomJoinRequestPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomJoinRequestPacketSerializer();

    public int RoomId { get; set; }

    public RoomJoinRequestPacket() { }

    public RoomJoinRequestPacket(int roomId) {
        RoomId = roomId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomJoinRequestPacket;
}