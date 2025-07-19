using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

public class RoomLeavePacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomLeavePacketSerializer();
    public int RoomId { get; set; }
    public RoomLeavePacket() { }
    public RoomLeavePacket(int roomId) { RoomId = roomId; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomLeavePacket;
}

public class RoomLeavePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomLeavePacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomLeavePacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        return stream.ToArray();
    }
} 