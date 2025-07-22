using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Server;

namespace Wrap.Shared.Network.Packets.Server;

public sealed class RoomInfoQueryPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomInfoQueryPacketSerializer();

    public int RoomId { get; set; }

    public RoomInfoQueryPacket() { }

    public RoomInfoQueryPacket(int roomId) {
        RoomId = roomId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomInfoQueryPacket;
}