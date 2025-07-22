using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

public class UserInfoResultPacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new UserInfoResultPacketSerializer();

    public UserInfo UserInfo { get; set; } = new UserInfo();
    public UserInfoResultPacket() { }
    public UserInfoResultPacket(UserInfo userInfo) { UserInfo = userInfo; }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.UserInfoResultPacket;
}