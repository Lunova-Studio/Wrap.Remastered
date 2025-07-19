using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class RoomJoinResultPacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomJoinResultPacketSerializer();
    public int RoomId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RoomJoinResultPacket() { }
    public RoomJoinResultPacket(int roomId, bool success, string message) { RoomId = roomId; Success = success; Message = message; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomJoinResultPacket;
}

public class RoomJoinResultPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomJoinResultPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.Success = stream.ReadInt32() != 0;
        packet.Message = stream.ReadString();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomJoinResultPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteInt32(p.Success ? 1 : 0);
        stream.WriteString(p.Message);
        return stream.ToArray();
    }
} 