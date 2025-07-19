using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class RoomOwnerChangedPacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomOwnerChangedPacketSerializer();
    public int RoomId { get; set; }
    public string NewOwnerUserId { get; set; } = string.Empty;
    public RoomOwnerChangedPacket() { }
    public RoomOwnerChangedPacket(int roomId, string newOwnerUserId) { RoomId = roomId; NewOwnerUserId = newOwnerUserId; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomOwnerChangedPacket;
}

public class RoomOwnerChangedPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomOwnerChangedPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.NewOwnerUserId = stream.ReadString();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomOwnerChangedPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.NewOwnerUserId);
        return stream.ToArray();
    }
} 