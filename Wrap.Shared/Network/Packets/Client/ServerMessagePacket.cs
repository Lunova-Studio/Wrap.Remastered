using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

public sealed class ServerMessagePacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new ServerMessagePacketSerializer();

    public string Message { get; set; } = string.Empty;

    public ServerMessagePacket(string message) {
        Message = message;
    }

    public ServerMessagePacket() { }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.ServerMessagePacket;
}