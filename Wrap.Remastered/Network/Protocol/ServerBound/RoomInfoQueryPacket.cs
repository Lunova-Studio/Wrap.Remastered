using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

public class RoomInfoQueryPacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomInfoQueryPacketSerializer();
    public int RoomId { get; set; }
    public RoomInfoQueryPacket() { }
    public RoomInfoQueryPacket(int roomId) { RoomId = roomId; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomInfoQueryPacket;
}

public class RoomInfoQueryPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomInfoQueryPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomInfoQueryPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        return stream.ToArray();
    }
} 