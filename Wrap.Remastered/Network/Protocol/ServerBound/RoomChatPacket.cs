using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

public class RoomChatPacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomChatPacketSerializer();
    public int RoomId { get; set; }
    public string Message { get; set; } = string.Empty;
    public RoomChatPacket() { }
    public RoomChatPacket(int roomId, string message) { RoomId = roomId; Message = message; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomChatPacket;
}

public class RoomChatPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomChatPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.Message = stream.ReadString();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomChatPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.Message);
        return stream.ToArray();
    }
} 