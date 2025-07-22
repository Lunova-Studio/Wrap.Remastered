using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

public sealed class RoomJoinResultPacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomJoinResultPacketSerializer();
    public int RoomId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public RoomJoinResultPacket() { }

    public RoomJoinResultPacket(int roomId, bool success, string message) {
        RoomId = roomId;
        Success = success;
        Message = message;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomJoinResultPacket;
}