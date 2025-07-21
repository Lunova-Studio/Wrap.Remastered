using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Network.Protocol.PeerBound;

public class PluginMessagePacket : IPeerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PluginMessagePacketSerializer();
    public NamespacedKey NamespacedKey { get; set; } = NamespacedKey.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public PluginMessagePacket(NamespacedKey namespacedKey, byte[] data)
    {
        if (data.Length > 4096)
            throw new ArgumentException("Data length exceeds 4096 bytes.");
        NamespacedKey = namespacedKey;
        Data = data;
    }

    public PluginMessagePacket() { }

    public ISerializer<IPacket> GetSerializer() => Serializer;

    public void OnSerialize(ref byte[] data) { }

    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.PluginMessage;
}

public class PluginMessagePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PluginMessagePacket packet = new();
        using MemoryStream stream = new(data);
        packet.NamespacedKey = NamespacedKey.Parse(stream.ReadString());
        int len = stream.ReadInt32();
        if (len > 4096) throw new ArgumentException("Data length exceeds 4096 bytes.");
        packet.Data = stream.ReadBytes(len);
        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PluginMessagePacket pluginPacket) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteString(pluginPacket.NamespacedKey.ToString());
        stream.WriteInt32(pluginPacket.Data.Length);
        stream.WriteBytes(pluginPacket.Data);
        return stream.ToArray();
    }
}
