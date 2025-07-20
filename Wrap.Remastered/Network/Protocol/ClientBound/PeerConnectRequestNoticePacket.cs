using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

/// <summary>
/// P2P连接请求通知包
/// </summary>
public class PeerConnectRequestNoticePacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectRequestNoticePacketSerializer();
    
    /// <summary>
    /// 请求者用户ID
    /// </summary>
    public string RequesterUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求者显示名称
    /// </summary>
    public string RequesterDisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求时间戳
    /// </summary>
    public long Timestamp { get; set; }

    public PeerConnectRequestNoticePacket() { }

    public PeerConnectRequestNoticePacket(string requesterUserId, string requesterDisplayName, long timestamp)
    {
        RequesterUserId = requesterUserId;
        RequesterDisplayName = requesterDisplayName;
        Timestamp = timestamp;
    }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 使用默认序列化
    }

    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PeerConnectRequestNoticePacket;
}

public class PeerConnectRequestNoticePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerConnectRequestNoticePacket packet = new();

        using MemoryStream stream = new(data);

        packet.RequesterUserId = stream.ReadString();
        packet.RequesterDisplayName = stream.ReadString();
        packet.Timestamp = stream.ReadLong();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerConnectRequestNoticePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.RequesterUserId);
        stream.WriteString(packet.RequesterDisplayName);
        stream.WriteLong(packet.Timestamp);

        return stream.ToArray();
    }
} 