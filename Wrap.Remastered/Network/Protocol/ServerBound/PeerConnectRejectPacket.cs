using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

/// <summary>
/// P2P连接拒绝包
/// </summary>
public class PeerConnectRejectPacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectRejectPacketSerializer();
    
    /// <summary>
    /// 请求者用户ID
    /// </summary>
    public string RequesterUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 拒绝原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    public PeerConnectRejectPacket() { }

    public PeerConnectRejectPacket(string requesterUserId, string reason)
    {
        RequesterUserId = requesterUserId;
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

    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.PeerConnectRejectPacket;
}

public class PeerConnectRejectPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerConnectRejectPacket packet = new();

        using MemoryStream stream = new(data);

        packet.RequesterUserId = stream.ReadString();
        packet.Reason = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerConnectRejectPacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.RequesterUserId);
        stream.WriteString(packet.Reason);

        return stream.ToArray();
    }
} 