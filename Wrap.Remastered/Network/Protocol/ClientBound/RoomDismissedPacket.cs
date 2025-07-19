using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class RoomDismissedPacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomDismissedPacketSerializer();
    public int RoomId { get; set; }
    public RoomDismissedPacket() { }
    public RoomDismissedPacket(int roomId) { RoomId = roomId; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomDismissedPacket;
}

public class RoomDismissedPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomDismissedPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomDismissedPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        return stream.ToArray();
    }
} 