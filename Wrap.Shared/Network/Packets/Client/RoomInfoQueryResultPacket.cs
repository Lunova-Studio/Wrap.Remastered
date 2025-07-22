using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

public sealed class RoomInfoQueryResultPacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomInfoQueryResultPacketSerializer();

    public int RoomId { get; set; }
    public int MaxUsers { get; set; }
    public int UserCount { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;

    public RoomInfoQueryResultPacket() { }
    public RoomInfoQueryResultPacket(int roomId, string roomName, string ownerUserId, int maxUsers, int userCount) {
        RoomId = roomId;
        RoomName = roomName;
        OwnerUserId = ownerUserId;
        MaxUsers = maxUsers;
        UserCount = userCount;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomInfoQueryResultPacket;
}
