using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

/// <summary>
/// P2P连接成功包
/// </summary>
public class PeerConnectSuccessPacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectSuccessPacketSerializer();

    /// <summary>
    /// 连接目标用户ID
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;

    public PeerConnectSuccessPacket() { }

    public PeerConnectSuccessPacket(string targetUserId)
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

    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.PeerConnectSuccessPacket;
}

public class PeerConnectSuccessPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerConnectSuccessPacket packet = new();

        using MemoryStream stream = new(data);

        packet.TargetUserId = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerConnectSuccessPacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.TargetUserId);

        return stream.ToArray();
    }
}