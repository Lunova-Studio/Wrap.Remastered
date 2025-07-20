using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class LoginSucceedPacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new LoginSucceedPacketSerializer();
    
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; } = DateTime.Now;
    public byte[] IPAddress { get; set; } = new byte[4];
    public int Port { get; set; }

    public LoginSucceedPacket(string userId, string name, string displayName, byte[] ipAddress, int port)
    {
        UserId = userId;
        Name = name;
        DisplayName = displayName;
        LoginTime = DateTime.Now;
        IPAddress = ipAddress;
        Port = port;
    }

    public LoginSucceedPacket(UserInfo userInfo)
    {
        UserId = userInfo.UserId;
        Name = userInfo.Name;
        DisplayName = userInfo.DisplayName;
        LoginTime = DateTime.Now;
    }

    public LoginSucceedPacket() { }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 可以在这里添加额外的序列化逻辑
    }

    public UserInfo AsUserInfo()
    {
        return new UserInfo
        {
            UserId = UserId,
            Name = Name,
            DisplayName = DisplayName
        };
    }

    public ClientBoundPacketType GetPacketType()
    {
        return ClientBoundPacketType.LoginSucceedPacket;
    }
}

public class LoginSucceedPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        LoginSucceedPacket packet = new();

        using MemoryStream stream = new(data);

        packet.UserId = stream.ReadString();
        packet.Name = stream.ReadString();
        packet.DisplayName = stream.ReadString();
        packet.LoginTime = DateTime.FromBinary(stream.ReadInt64());
        packet.IPAddress = stream.ReadBytes(4);
        packet.Port = stream.ReadInt32();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not LoginSucceedPacket loginSucceedPacket) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(loginSucceedPacket.UserId);
        stream.WriteString(loginSucceedPacket.Name);
        stream.WriteString(loginSucceedPacket.DisplayName);
        stream.WriteInt64(loginSucceedPacket.LoginTime.ToBinary());
        stream.WriteBytes(loginSucceedPacket.IPAddress);
        stream.WriteInt32(loginSucceedPacket.Port);

        return stream.ToArray();
    }
}
