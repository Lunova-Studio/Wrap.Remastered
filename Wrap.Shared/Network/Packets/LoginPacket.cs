using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

public sealed class LoginPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new LoginPacketSerializer();

    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public LoginPacket(string name, string displayName) {
        Name = name;
        DisplayName = displayName;
    }

    public LoginPacket(UserInfo userInfo) {
        Name = userInfo.Name;
        DisplayName = userInfo.DisplayName;
    }

    public LoginPacket() { }

    public ISerializer<IPacket> GetSerializer() {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data) {

    }

    public UserInfo AsUserInfo() {
        return new UserInfo {
            Name = Name,
            DisplayName = DisplayName
        };
    }

    public ServerBoundPacketType GetPacketType() {
        return ServerBoundPacketType.LoginPacket;
    }
}
