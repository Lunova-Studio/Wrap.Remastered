using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class LoginFailedPacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new LoginFailedPacketSerializer();
    
    public int ErrorCode { get; set; } = 0;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime FailTime { get; set; } = DateTime.Now;

    public LoginFailedPacket(int errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        FailTime = DateTime.Now;
    }

    public LoginFailedPacket() { }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 可以在这里添加额外的序列化逻辑
    }

    public ClientBoundPacketType GetPacketType()
    {
        return ClientBoundPacketType.LoginFailedPacket;
    }
}

public class LoginFailedPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        LoginFailedPacket packet = new();

        using MemoryStream stream = new(data);

        packet.ErrorCode = stream.ReadInt32();
        packet.ErrorMessage = stream.ReadString();
        packet.FailTime = DateTime.FromBinary(stream.ReadInt64());

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not LoginFailedPacket loginFailedPacket) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteInt32(loginFailedPacket.ErrorCode);
        stream.WriteString(loginFailedPacket.ErrorMessage);
        stream.WriteInt64(loginFailedPacket.FailTime.ToBinary());

        return stream.ToArray();
    }
}
