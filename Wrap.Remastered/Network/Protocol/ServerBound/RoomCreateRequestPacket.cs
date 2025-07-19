using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

public class RoomCreateRequestPacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomCreateRequestPacketSerializer();

    public string RoomName { get; set; } = string.Empty;
    public int MaxUsers { get; set; } = 10;

    public RoomCreateRequestPacket() { }
    public RoomCreateRequestPacket(string roomName, int maxUsers = 10)
    {
        RoomName = roomName;
        MaxUsers = maxUsers;
    }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomCreateRequestPacket;
}

public class RoomCreateRequestPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomCreateRequestPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomName = stream.ReadString();
        packet.MaxUsers = stream.ReadInt32();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomCreateRequestPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteString(p.RoomName);
        stream.WriteInt32(p.MaxUsers);
        return stream.ToArray();
    }
} 