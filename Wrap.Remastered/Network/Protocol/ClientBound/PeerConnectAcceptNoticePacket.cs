using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.ClientBound;

/// <summary>
/// P2P连接接受通知包
/// </summary>
public class PeerConnectAcceptNoticePacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectAcceptNoticePacketSerializer();
    
    /// <summary>
    /// 接受者用户ID
    /// </summary>
    public string AccepterUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 接受者显示名称
    /// </summary>
    public string AccepterDisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 接受时间戳
    /// </summary>
    public long Timestamp { get; set; }

    public PeerConnectAcceptNoticePacket() { }

    public PeerConnectAcceptNoticePacket(string accepterUserId, string accepterDisplayName, long timestamp)
    {
        AccepterUserId = accepterUserId;
        AccepterDisplayName = accepterDisplayName;
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

    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PeerConnectAcceptNoticePacket;
}

public class PeerConnectAcceptNoticePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerConnectAcceptNoticePacket packet = new();

        using MemoryStream stream = new(data);

        packet.AccepterUserId = stream.ReadString();
        packet.AccepterDisplayName = stream.ReadString();
        packet.Timestamp = stream.ReadLong();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerConnectAcceptNoticePacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.AccepterUserId);
        stream.WriteString(packet.AccepterDisplayName);
        stream.WriteLong(packet.Timestamp);

        return stream.ToArray();
    }
} 