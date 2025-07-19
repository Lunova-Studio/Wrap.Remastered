using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Schemas;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

public class LoginPacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new LoginPacketSerializer();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public LoginPacket(string userId, string name, string displayName)
    {
        UserId = userId;
        Name = name;
        DisplayName = displayName;
    }

    public LoginPacket(UserInfo userInfo)
    {
        UserId = userInfo.UserId;
        Name = userInfo.Name;
        DisplayName = userInfo.DisplayName;
    }

    public LoginPacket() { }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {

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

    public ServerBoundPacketType GetPacketType()
    {
        return ServerBoundPacketType.LoginPacket;
    }
}

public class LoginPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        LoginPacket packet = new();

        using MemoryStream stream = new(data);

        packet.UserId = stream.ReadString();
        packet.Name = stream.ReadString();
        packet.DisplayName = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not LoginPacket loginPacket) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(loginPacket.UserId);
        stream.WriteString(loginPacket.Name);
        stream.WriteString(loginPacket.DisplayName);

        return stream.ToArray();
    }
}
