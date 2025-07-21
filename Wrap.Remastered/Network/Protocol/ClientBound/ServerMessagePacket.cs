using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class ServerMessagePacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new ServerMessagePacketSerializer();
    public string Message { get; set; } = string.Empty;

    public ServerMessagePacket(string message)
    {
        Message = message;
    }

    public ServerMessagePacket() { }

    public ISerializer<IPacket> GetSerializer() => Serializer;

    public void OnSerialize(ref byte[] data) { }

    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.ServerMessagePacket;
}

public class ServerMessagePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        ServerMessagePacket packet = new();
        using MemoryStream stream = new(data);
        packet.Message = stream.ReadString();
        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not ServerMessagePacket msgPacket) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteString(msgPacket.Message);
        return stream.ToArray();
    }
} 