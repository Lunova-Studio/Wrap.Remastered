using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

public class LoginSucceedPacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new LoginSucceedPacketSerializer();

    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; } = DateTime.Now;
    public byte[] IPAddress { get; set; } = new byte[4];
    public int Port { get; set; }

    public LoginSucceedPacket(string userId, string name, string displayName, byte[] ipAddress, int port) {
        UserId = userId;
        Name = name;
        DisplayName = displayName;
        LoginTime = DateTime.Now;
        IPAddress = ipAddress;
        Port = port;
    }

    public LoginSucceedPacket(UserInfo userInfo) {
        UserId = userInfo.UserId;
        Name = userInfo.Name;
        DisplayName = userInfo.DisplayName;
        LoginTime = DateTime.Now;
    }

    public LoginSucceedPacket() { }

    public ISerializer<IPacket> GetSerializer() {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data) {
        // 可以在这里添加额外的序列化逻辑
    }

    public UserInfo AsUserInfo() {
        return new UserInfo {
            UserId = UserId,
            Name = Name,
            DisplayName = DisplayName
        };
    }

    public ClientBoundPacketType GetPacketType() {
        return ClientBoundPacketType.LoginSucceedPacket;
    }
}