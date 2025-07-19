using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ServerBound;

namespace Wrap.Remastered.Network.Protocol;

public class UnsolvedPacket : IPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new UnsolvedPacketSerializer();

    public int PacketType { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public UnsolvedPacket() { }

    public UnsolvedPacket(int packetType, byte[] data)
    {
        PacketType = packetType;
        Data = data;
    }

    public void OnSerialize(ref byte[] data)
    {
        
    }
}

public class UnsolvedPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        UnsolvedPacket packet = new();

        using MemoryStream stream = new(data);

        packet.PacketType = stream.ReadInt32();
        int len = stream.ReadVarInt();
        stream.Read(packet.Data, 0, len);

        return packet;
    }
    public byte[] Serialize(IPacket obj)
    {
        if (obj is not UnsolvedPacket unsolvedPacket) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteInt32(unsolvedPacket.PacketType);
        stream.WriteVarInt(unsolvedPacket.Data.Length);
        stream.Write(unsolvedPacket.Data);

        return stream.ToArray();
    }
}
