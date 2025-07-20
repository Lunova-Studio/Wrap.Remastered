using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

/// <summary>
/// P2P连接拒绝通知包
/// </summary>
public class PeerConnectRejectNoticePacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectRejectNoticePacketSerializer();
    
    /// <summary>
    /// 拒绝者用户ID
    /// </summary>
    public string RejecterUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 拒绝者显示名称
    /// </summary>
    public string RejecterDisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 拒绝原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// 拒绝时间戳
    /// </summary>
    public long Timestamp { get; set; }

    public PeerConnectRejectNoticePacket() { }

    public PeerConnectRejectNoticePacket(string rejecterUserId, string rejecterDisplayName, string reason, long timestamp)
    {
        RejecterUserId = rejecterUserId;
        RejecterDisplayName = rejecterDisplayName;
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

    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PeerConnectRejectNoticePacket;
}

public class PeerConnectRejectNoticePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerConnectRejectNoticePacket packet = new();

        using MemoryStream stream = new(data);

        packet.RejecterUserId = stream.ReadString();
        packet.RejecterDisplayName = stream.ReadString();
        packet.Reason = stream.ReadString();
        packet.Timestamp = stream.ReadLong();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerConnectRejectNoticePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.RejecterUserId);
        stream.WriteString(packet.RejecterDisplayName);
        stream.WriteString(packet.Reason);
        stream.WriteLong(packet.Timestamp);

        return stream.ToArray();
    }
} 