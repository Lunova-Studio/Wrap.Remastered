using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Server;

namespace Wrap.Shared.Network.Packets.Server;

public sealed class RoomJoinApprovePacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomJoinApprovePacketSerializer();

    public int RoomId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public RoomJoinApprovePacket() { }

    public RoomJoinApprovePacket(int roomId, string userId) {
        RoomId = roomId;
        UserId = userId;
    }

    public ISerializer<IPacket> GetSerializer() => Serializer;

    public void OnSerialize(ref byte[] data) { }

    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomJoinApprovePacket;
}