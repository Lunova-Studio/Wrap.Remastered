using System;
using System.Collections.Generic;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Server.Managers;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class RoomInfoPacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomInfoPacketSerializer();

    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public UserInfo Owner { get; set; } = new UserInfo();
    public List<UserInfo> Users { get; set; } = new();
    public int MaxUsers { get; set; }

    public RoomInfoPacket() { }
    public RoomInfoPacket(Room room)
    {
        RoomId = room.Id;
        RoomName = room.Name;
        Owner = room.Owner;
        Users = new List<UserInfo>(room.Users);
        MaxUsers = room.MaxUsers;
    }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomInfoPacket;
}

public class RoomInfoPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomInfoPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.RoomName = stream.ReadString();
        packet.Owner = ReadUserInfo(stream);
        int userCount = stream.ReadInt32();
        for (int i = 0; i < userCount; i++)
        {
            packet.Users.Add(ReadUserInfo(stream));
        }
        packet.MaxUsers = stream.ReadInt32();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomInfoPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.RoomName);
        WriteUserInfo(stream, p.Owner);
        stream.WriteInt32(p.Users.Count);
        foreach (var user in p.Users)
        {
            WriteUserInfo(stream, user);
        }
        stream.WriteInt32(p.MaxUsers);
        return stream.ToArray();
    }
    private static UserInfo ReadUserInfo(Stream stream)
    {
        return new UserInfo
        {
            UserId = stream.ReadString(),
            Name = stream.ReadString(),
            DisplayName = stream.ReadString()
        };
    }
    private static void WriteUserInfo(Stream stream, UserInfo user)
    {
        stream.WriteString(user.UserId);
        stream.WriteString(user.Name);
        stream.WriteString(user.DisplayName);
    }
} 