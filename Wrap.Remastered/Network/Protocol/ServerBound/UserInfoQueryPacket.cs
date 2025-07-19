using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

public class UserInfoQueryPacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new UserInfoQueryPacketSerializer();
    public string UserId { get; set; } = string.Empty;
    public UserInfoQueryPacket() { }
    public UserInfoQueryPacket(string userId) { UserId = userId; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.UserInfoQueryPacket;
}

public class UserInfoQueryPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        UserInfoQueryPacket packet = new();
        using MemoryStream stream = new(data);
        packet.UserId = stream.ReadString();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not UserInfoQueryPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteString(p.UserId);
        return stream.ToArray();
    }
} 