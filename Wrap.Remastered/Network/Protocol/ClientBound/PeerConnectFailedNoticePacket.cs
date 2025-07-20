using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

/// <summary>
/// P2P连接失败包
/// </summary>
public class PeerConnectFailedNoticePacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectFailedNoticePacketSerializer();
    
    /// <summary>
    /// 连接目标用户ID
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 失败原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// 失败时间戳
    /// </summary>
    public long Timestamp { get; set; }

    public PeerConnectFailedNoticePacket() { }

    public PeerConnectFailedNoticePacket(string targetUserId, string reason, long timestamp)
    {
        TargetUserId = targetUserId;
        Reason = reason;
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

    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PeerConnectFailedNoticePacket;
}

public class PeerConnectFailedNoticePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerConnectFailedNoticePacket packet = new();

        using MemoryStream stream = new(data);

        packet.TargetUserId = stream.ReadString();
        packet.Reason = stream.ReadString();
        packet.Timestamp = stream.ReadLong();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerConnectFailedNoticePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.TargetUserId);
        stream.WriteString(packet.Reason);
        stream.WriteLong(packet.Timestamp);

        return stream.ToArray();
    }
} 