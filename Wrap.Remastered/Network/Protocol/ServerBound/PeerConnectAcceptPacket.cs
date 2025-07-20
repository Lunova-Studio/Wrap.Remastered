using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

/// <summary>
/// P2P连接接受包
/// </summary>
public class PeerConnectAcceptPacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectAcceptPacketSerializer();
    
    /// <summary>
    /// 请求者用户ID
    /// </summary>
    public string RequesterUserId { get; set; } = string.Empty;

    public PeerConnectAcceptPacket() { }

    public PeerConnectAcceptPacket(string requesterUserId)
    {
        RequesterUserId = requesterUserId;
    }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 使用默认序列化
    }

    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.PeerConnectAcceptPacket;
}

public class PeerConnectAcceptPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerConnectAcceptPacket packet = new();

        using MemoryStream stream = new(data);

        packet.RequesterUserId = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerConnectAcceptPacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.RequesterUserId);

        return stream.ToArray();
    }
} 