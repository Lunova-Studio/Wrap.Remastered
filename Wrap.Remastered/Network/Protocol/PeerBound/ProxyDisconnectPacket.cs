using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.PeerBound;

/// <summary>
/// 代理断开连接包
/// </summary>
public class ProxyDisconnectPacket : IPeerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new ProxyDisconnectPacketSerializer();
    
    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 断开原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    public ProxyDisconnectPacket() { }

    public ProxyDisconnectPacket(string connectionId, string reason = "")
    {
        ConnectionId = connectionId;
        Reason = reason;
    }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 使用默认序列化
    }

    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.ProxyDisconnectPacket;
}

public class ProxyDisconnectPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        ProxyDisconnectPacket packet = new();

        using MemoryStream stream = new(data);

        packet.ConnectionId = stream.ReadString();
        packet.Reason = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not ProxyDisconnectPacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.ConnectionId);
        stream.WriteString(packet.Reason);

        return stream.ToArray();
    }
} 