using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Server;

namespace Wrap.Shared.Network.Packets.Server;

public sealed class RoomTransferOwnerPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomTransferOwnerPacketSerializer();

    public int RoomId { get; set; }
    public string NewOwnerUserId { get; set; } = string.Empty;

    public RoomTransferOwnerPacket() { }

    public RoomTransferOwnerPacket(int roomId, string newOwnerUserId) {
        RoomId = roomId;
        NewOwnerUserId = newOwnerUserId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomTransferOwnerPacket;
}