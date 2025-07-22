using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

public sealed class RoomChatMessagePacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomChatMessagePacketSerializer();
    public int RoomId { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public RoomChatMessagePacket() { }

    public RoomChatMessagePacket(int roomId, string senderUserId, string senderDisplayName, string message, DateTime timestamp) {
        RoomId = roomId;
        SenderUserId = senderUserId;
        SenderDisplayName = senderDisplayName;
        Message = message;
        Timestamp = timestamp;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomChatMessagePacket;
}