using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public class UserInfoResultPacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new UserInfoResultPacketSerializer();

    public UserInfo UserInfo { get; set; } = new UserInfo();
    public UserInfoResultPacket() { }
    public UserInfoResultPacket(UserInfo userInfo) { UserInfo = userInfo; }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.UserInfoResultPacket;
}