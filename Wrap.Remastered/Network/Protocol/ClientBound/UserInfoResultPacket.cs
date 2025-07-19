using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class UserInfoResultPacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new UserInfoResultPacketSerializer();
    public UserInfo UserInfo { get; set; } = new UserInfo();
    public UserInfoResultPacket() { }
    public UserInfoResultPacket(UserInfo userInfo) { UserInfo = userInfo; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.UserInfoResultPacket;
}

public class UserInfoResultPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        UserInfoResultPacket packet = new();
        using MemoryStream stream = new(data);
        packet.UserInfo = new UserInfo
        {
            UserId = stream.ReadString(),
            Name = stream.ReadString(),
            DisplayName = stream.ReadString()
        };
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not UserInfoResultPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteString(p.UserInfo.UserId);
        stream.WriteString(p.UserInfo.Name);
        stream.WriteString(p.UserInfo.DisplayName);
        return stream.ToArray();
    }
} 