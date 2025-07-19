using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

public class RoomJoinApprovePacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomJoinApprovePacketSerializer();
    public int RoomId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public RoomJoinApprovePacket() { }
    public RoomJoinApprovePacket(int roomId, string userId) { RoomId = roomId; UserId = userId; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomJoinApprovePacket;
}

public class RoomJoinApprovePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomJoinApprovePacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.UserId = stream.ReadString();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomJoinApprovePacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.UserId);
        return stream.ToArray();
    }
} 