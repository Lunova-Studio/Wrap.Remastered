using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Server;

namespace Wrap.Shared.Network.Packets.Server;

public sealed class RoomLeavePacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomLeavePacketSerializer();

    public int RoomId { get; set; }

    public RoomLeavePacket() { }

    public RoomLeavePacket(int roomId) {
        RoomId = roomId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomLeavePacket;
}
