using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Shared.Network.Serializers.Client;

public sealed class UserInfoResultPacketSerializer : ISerializer<IPacket> {
    public IPacket Deserialize(byte[] data) {
        UserInfoResultPacket packet = new();
        using MemoryStream stream = new(data);
        packet.UserInfo = new UserInfo {
            UserId = stream.ReadString(),
            Name = stream.ReadString(),
            DisplayName = stream.ReadString()
        };
        return packet;
    }
    public byte[] Serialize(IPacket obj) {
        if (obj is not UserInfoResultPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteString(p.UserInfo.UserId);
        stream.WriteString(p.UserInfo.Name);
        stream.WriteString(p.UserInfo.DisplayName);
        return stream.ToArray();
    }
}