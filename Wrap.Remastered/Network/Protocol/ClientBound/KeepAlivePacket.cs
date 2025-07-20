using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class KeepAlivePacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new KeepAlivePacketSerializer();
    public int Value { get; set; }
    public KeepAlivePacket() { }
    public KeepAlivePacket(int value) { Value = value; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.KeepAlivePacket;
}

public class KeepAlivePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        KeepAlivePacket packet = new();
        using MemoryStream stream = new(data);
        packet.Value = stream.ReadInt32();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not KeepAlivePacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.Value);
        return stream.ToArray();
    }
} 