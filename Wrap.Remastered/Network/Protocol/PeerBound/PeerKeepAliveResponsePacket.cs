using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.PeerBound;

/// <summary>
/// P2P心跳响应包
/// </summary>
public class PeerKeepAliveResponsePacket : IPeerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerKeepAliveResponsePacketSerializer();
    
    /// <summary>
    /// 心跳值（与请求包中的值相同）
    /// </summary>
    public int Value { get; set; }

    public PeerKeepAliveResponsePacket() 
    {
        Value = 0;
    }

    public PeerKeepAliveResponsePacket(int value)
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

    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.PeerKeepAliveResponsePacket;
}

public class PeerKeepAliveResponsePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerKeepAliveResponsePacket packet = new();

        using MemoryStream stream = new(data);

        packet.Value = stream.ReadInt();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerKeepAliveResponsePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteInt(packet.Value);

        return stream.ToArray();
    }
} 