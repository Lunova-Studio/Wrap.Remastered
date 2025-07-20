using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Extensions;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ClientBound;

namespace Wrap.Remastered.Network.Protocol.ServerBound;

/// <summary>
/// P2P连接失败包
/// </summary>
public class PeerConnectFailedPacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectFailedPacketSerializer();

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

    public PeerConnectFailedPacket() { }

    public PeerConnectFailedPacket(string targetUserId, string reason, long timestamp)
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

    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.PeerConnectFailedPacket;
}

public class PeerConnectFailedPacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        PeerConnectFailedPacket packet = new();

        using MemoryStream stream = new(data);

        packet.TargetUserId = stream.ReadString();
        packet.Reason = stream.ReadString();
        packet.Timestamp = stream.ReadLong();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not PeerConnectFailedPacket packet) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(packet.TargetUserId);
        stream.WriteString(packet.Reason);
        stream.WriteLong(packet.Timestamp);

        return stream.ToArray();
    }
}
