using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public sealed class RoomCreateRequestPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomCreateRequestPacketSerializer();

    public int MaxUsers { get; set; } = 10;
    public string RoomName { get; set; } = string.Empty;

    public RoomCreateRequestPacket() { }

    public RoomCreateRequestPacket(string roomName, int maxUsers = 10) {
        RoomName = roomName;
        MaxUsers = maxUsers;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomCreateRequestPacket;
}