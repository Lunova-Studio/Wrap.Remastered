using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.PeerBound;

/// <summary>
/// 代理响应包
/// </summary>
public class ProxyResponsePacket : IPeerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new ProxyResponsePacketSerializer();
    
    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    public ProxyResponsePacket() { }

    public ProxyResponsePacket(string connectionId, bool success, string errorMessage = "")
    {
        ConnectionId = connectionId;
        Success = success;
        ErrorMessage = errorMessage;
    }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 使用默认序列化
    }

    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.ProxyResponsePacket;
}

public class ProxyResponsePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        ProxyResponsePacket packet = new();

        using MemoryStream stream = new(data);

        packet.ConnectionId = stream.ReadString();
        packet.Success = stream.ReadBool();
        packet.ErrorMessage = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not ProxyResponsePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.ConnectionId);
        stream.WriteBool(packet.Success);
        stream.WriteString(packet.ErrorMessage);

        return stream.ToArray();
    }
} 