using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

public sealed class PluginMessagePacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new PluginMessagePacketSerializer();

    public NamespacedKey NamespacedKey { get; set; } = NamespacedKey.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public PluginMessagePacket(NamespacedKey namespacedKey, byte[] data) {
        if (data.Length > 4096)
            throw new ArgumentException("Data length exceeds 4096 bytes.");
        NamespacedKey = namespacedKey;
        Data = data;
    }

    public PluginMessagePacket() { }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PluginMessage;
}
