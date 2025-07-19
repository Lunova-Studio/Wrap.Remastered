using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class RoomInfoQueryResultPacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomInfoQueryResultPacketSerializer();
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int UserCount { get; set; }
    public RoomInfoQueryResultPacket() { }
    public RoomInfoQueryResultPacket(int roomId, string roomName, string ownerUserId, int maxUsers, int userCount)
    {
        RoomId = roomId;
        RoomName = roomName;
        OwnerUserId = ownerUserId;
        MaxUsers = maxUsers;
        UserCount = userCount;
    }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomInfoQueryResultPacket;
}

public class RoomInfoQueryResultPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomInfoQueryResultPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.RoomName = stream.ReadString();
        packet.OwnerUserId = stream.ReadString();
        packet.MaxUsers = stream.ReadInt32();
        packet.UserCount = stream.ReadInt32();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomInfoQueryResultPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.RoomName);
        stream.WriteString(p.OwnerUserId);
        stream.WriteInt32(p.MaxUsers);
        stream.WriteInt32(p.UserCount);
        return stream.ToArray();
    }
}
