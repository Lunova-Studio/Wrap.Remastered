using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public sealed class RoomJoinRejectPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomJoinRejectPacketSerializer();

    public int RoomId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public RoomJoinRejectPacket() { }

    public RoomJoinRejectPacket(int roomId, string userId) {
        RoomId = roomId;
        UserId = userId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomJoinRejectPacket;
}