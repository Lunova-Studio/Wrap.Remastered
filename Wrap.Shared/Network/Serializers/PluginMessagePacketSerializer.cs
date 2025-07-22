using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Packets;

namespace Wrap.Shared.Network.Serializers;

public sealed class PluginMessagePacketSerializer : ISerializer<IPacket> {
    public byte[] Serialize(IPacket obj) {
        if (obj is not PluginMessagePacket pluginPacket) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteString(pluginPacket.NamespacedKey.ToString());
        stream.WriteInt32(pluginPacket.Data.Length);
        stream.WriteBytes(pluginPacket.Data);
        return stream.ToArray();
    }

    public IPacket Deserialize(byte[] data) {
        PluginMessagePacket packet = new();
        using MemoryStream stream = new(data);
        packet.NamespacedKey = NamespacedKey.Parse(stream.ReadString());
        int len = stream.ReadInt32();
        if (len > 4096) throw new ArgumentException("Data length exceeds 4096 bytes.");
        packet.Data = stream.ReadBytes(len);
        return packet;
    }
}