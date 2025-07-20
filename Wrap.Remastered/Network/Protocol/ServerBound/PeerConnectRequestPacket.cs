using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

/// <summary>
/// P2P连接请求包
/// </summary>
public class PeerConnectRequestPacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectRequestPacketSerializer();
    
    /// <summary>
    /// 目标用户ID
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;

    public PeerConnectRequestPacket() { }

    public PeerConnectRequestPacket(string targetUserId)
    {
        TargetUserId = targetUserId;
    }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 使用默认序列化
    }

    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.PeerConnectRequestPacket;
}

public class PeerConnectRequestPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerConnectRequestPacket packet = new();

        using MemoryStream stream = new(data);

        packet.TargetUserId = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerConnectRequestPacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.TargetUserId);

        return stream.ToArray();
    }
} 