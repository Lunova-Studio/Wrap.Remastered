using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.PeerBound;

/// <summary>
/// 代理连接请求包
/// </summary>
public class ProxyConnectPacket : IPeerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new ProxyConnectPacketSerializer();

    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    public ProxyConnectPacket() { }

    public ProxyConnectPacket(string connectionId)
    {
        ConnectionId = connectionId;
    }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 使用默认序列化
    }

    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.ProxyConnectPacket;
}

public class ProxyConnectPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        ProxyConnectPacket packet = new();

        using MemoryStream stream = new(data);

        packet.ConnectionId = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not ProxyConnectPacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.ConnectionId);

        return stream.ToArray();
    }
}