using System;
using System.IO;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

public class RoomTransferOwnerPacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new RoomTransferOwnerPacketSerializer();
    public int RoomId { get; set; }
    public string NewOwnerUserId { get; set; } = string.Empty;
    public RoomTransferOwnerPacket() { }
    public RoomTransferOwnerPacket(int roomId, string newOwnerUserId) { RoomId = roomId; NewOwnerUserId = newOwnerUserId; }
    public ISerializer<IPacket> GetSerializer() => Serializer;
    public void OnSerialize(ref byte[] data) { }
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.RoomTransferOwnerPacket;
}

public class RoomTransferOwnerPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        RoomTransferOwnerPacket packet = new();
        using MemoryStream stream = new(data);
        packet.RoomId = stream.ReadInt32();
        packet.NewOwnerUserId = stream.ReadString();
        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not RoomTransferOwnerPacket p) throw new ArgumentException();
        using MemoryStream stream = new();
        stream.WriteInt32(p.RoomId);
        stream.WriteString(p.NewOwnerUserId);
        return stream.ToArray();
    }
} 