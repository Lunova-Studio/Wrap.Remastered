using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

public class RoomJoinRequestNoticePacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomJoinRequestNoticePacketSerializer();
    public int RoomId { get; set; }
    public string ApplicantUserId { get; set; } = string.Empty;
    public RoomJoinRequestNoticePacket() { }
    public RoomJoinRequestNoticePacket(int roomId, string applicantUserId) { RoomId = roomId; ApplicantUserId = applicantUserId; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.RoomJoinRequestNoticePacket;
}

public class RoomJoinRequestNoticePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomJoinRequestNoticePacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.ApplicantUserId = stream.ReadString();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomJoinRequestNoticePacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.ApplicantUserId);
        return stream.ToArray();
    }
} 