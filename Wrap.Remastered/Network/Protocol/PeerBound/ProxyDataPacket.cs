using ConsoleInteractive;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.PeerBound;

/// <summary>
/// 代理数据包
/// </summary>
public class ProxyDataPacket : IPeerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new ProxyDataPacketSerializer();
    
    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 数据
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();
    
    /// <summary>
    /// 数据方向 (true: 客户端到服务器, false: 服务器到客户端)
    /// </summary>
    public bool IsClientToServer { get; set; }

    public ProxyDataPacket() { }

    public ProxyDataPacket(string connectionId, byte[] data, bool isClientToServer)
    {
        ConnectionId = connectionId;
        Data = data;
        IsClientToServer = isClientToServer;
    }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 使用默认序列化
    }

    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.ProxyDataPacket;
}

public class ProxyDataPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        ProxyDataPacket packet = new();

        using MemoryStream stream = new(data);

        packet.ConnectionId = stream.ReadString();
        int dataLength = stream.ReadInt();
        packet.Data = stream.ReadBytes(dataLength);
        packet.IsClientToServer = stream.ReadBool();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not ProxyDataPacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.ConnectionId);
        stream.WriteInt(packet.Data.Length);
        stream.WriteBytes(packet.Data);
        stream.WriteBool(packet.IsClientToServer);

        var result = stream.ToArray();
        
        return result;
    }
} 