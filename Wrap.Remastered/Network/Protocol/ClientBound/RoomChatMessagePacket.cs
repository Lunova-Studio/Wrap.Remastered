using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class RoomChatMessagePacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomChatMessagePacketSerializer();
    public int RoomId { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public RoomChatMessagePacket() { }
    public RoomChatMessagePacket(int roomId, string senderUserId, string senderDisplayName, string message, DateTime timestamp)
    {
        RoomId = roomId;
        SenderUserId = senderUserId;
        SenderDisplayName = senderDisplayName;
        Message = message;
        Timestamp = timestamp;
    }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomChatMessagePacket;
}

public class RoomChatMessagePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomChatMessagePacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.SenderUserId = stream.ReadString();
        packet.SenderDisplayName = stream.ReadString();
        packet.Message = stream.ReadString();
        packet.Timestamp = DateTime.FromBinary(stream.ReadInt64());
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomChatMessagePacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.SenderUserId);
        stream.WriteString(p.SenderDisplayName);
        stream.WriteString(p.Message);
        stream.WriteInt64(p.Timestamp.ToBinary());
        return stream.ToArray();
    }
} 