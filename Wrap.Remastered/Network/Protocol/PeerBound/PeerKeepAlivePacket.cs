using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.PeerBound;

/// <summary>
/// P2P心跳包
/// </summary>
public class PeerKeepAlivePacket : IPeerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerKeepAlivePacketSerializer();
    
    /// <summary>
    /// 心跳值
    /// </summary>
    public int Value { get; set; }

    public PeerKeepAlivePacket() 
    {
        Value = Random.Shared.Next();
    }

    public PeerKeepAlivePacket(int value)
    {
        Value = value;
    }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 使用默认序列化
    }

    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.PeerKeepAlivePacket;
}

public class PeerKeepAlivePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerKeepAlivePacket packet = new();

        using MemoryStream stream = new(data);

        packet.Value = stream.ReadInt();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerKeepAlivePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteInt(packet.Value);

        return stream.ToArray();
    }
} 