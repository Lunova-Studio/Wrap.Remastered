using Wrap.Shared.Extensions;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Shared.Network.Serializers.Client;

public sealed class RoomInfoPacketSerializer : ISerializer<IPacket> {
    public IPacket Deserialize(byte[] data) {
        RoomInfoPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.RoomName = stream.ReadString();
        packet.Owner = ReadUserInfo(stream);
        int userCount = stream.ReadInt32();
        for (int i = 0; i < userCount; i++) {
            packet.Users.Add(ReadUserInfo(stream));
        }
        packet.MaxUsers = stream.ReadInt32();
        return packet;
    }
    public byte[] Serialize(IPacket obj) {
        if (obj is not RoomInfoPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.RoomName);
        WriteUserInfo(stream, p.Owner);
        stream.WriteInt32(p.Users.Count);
        foreach (var user in p.Users) {
            WriteUserInfo(stream, user);
        }
        stream.WriteInt32(p.MaxUsers);
        return stream.ToArray();
    }
    private static UserInfo ReadUserInfo(Stream stream) {
        return new UserInfo {
            UserId = stream.ReadString(),
            Name = stream.ReadString(),
            DisplayName = stream.ReadString()
        };
    }
    private static void WriteUserInfo(Stream stream, UserInfo user) {
        stream.WriteString(user.UserId);
        stream.WriteString(user.Name);
        stream.WriteString(user.DisplayName);
    }
}