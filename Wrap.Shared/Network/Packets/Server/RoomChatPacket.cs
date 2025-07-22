using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Server;

namespace Wrap.Shared.Network.Packets.Server;

public sealed class RoomChatPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomChatPacketSerializer();

    public int RoomId { get; set; }
    public string Message { get; set; } = string.Empty;

    public RoomChatPacket() { }

    public RoomChatPacket(int roomId, string message) {
        RoomId = roomId;
        Message = message;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomChatPacket;
}
