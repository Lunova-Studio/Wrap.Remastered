using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

/// <summary>
/// P2P连接失败包
/// </summary>
public sealed class PeerConnectFailedPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectFailedPacketSerializer();

    /// <summary>
    /// 连接目标用户 ID
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>
    /// 失败原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 失败时间戳（Unix 时间，秒）
    /// </summary>
    public long Timestamp { get; set; }

    public PeerConnectFailedPacket() { }

    public PeerConnectFailedPacket(string targetUserId, string reason, long timestamp) {
        TargetUserId = targetUserId;
        Reason = reason;
        Timestamp = timestamp;
    }


    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.PeerConnectFailedPacket;
}