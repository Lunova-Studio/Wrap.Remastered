using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public sealed class UserInfoQueryPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new UserInfoQueryPacketSerializer();

    public string UserId { get; set; } = string.Empty;

    public UserInfoQueryPacket() { }

    public UserInfoQueryPacket(string userId) {
        UserId = userId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.UserInfoQueryPacket;
}
