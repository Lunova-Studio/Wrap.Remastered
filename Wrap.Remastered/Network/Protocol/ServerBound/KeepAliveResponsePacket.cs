using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

public class KeepAliveResponsePacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new KeepAliveResponsePacketSerializer();
    public int Value { get; set; }
    public KeepAliveResponsePacket() { }
    public KeepAliveResponsePacket(int value) { Value = value; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.KeepAliveResponsePacket;
}

public class KeepAliveResponsePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        KeepAliveResponsePacket packet = new();
        using MemoryStream stream = new(data);
        packet.Value = stream.ReadInt32();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not KeepAliveResponsePacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.Value);
        return stream.ToArray();
    }
} 