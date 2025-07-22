using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public sealed class RoomInfoPacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new RoomInfoPacketSerializer();

    public int RoomId { get; set; }
    public int MaxUsers { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public UserInfo Owner { get; set; } = new UserInfo();
    public List<UserInfo> Users { get; set; } = [];

    public ISerializer<IPacket> GetSerializer() => Serializer;

    public RoomInfoPacket() { }

    public RoomInfoPacket(Room room) {
        RoomId = room.Id;
        RoomName = room.Name;
        Owner = room.Owner;
        Users = new List<UserInfo>(room.Users.Values);
        MaxUsers = room.MaxUsers;
    }

    public void OnSerialize(ref byte[] data) { }

    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomInfoPacket;
}
