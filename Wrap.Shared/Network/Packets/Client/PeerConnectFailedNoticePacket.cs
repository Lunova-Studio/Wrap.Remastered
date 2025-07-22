using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

/// <summary>
/// P2P连接失败包
/// </summary>
public sealed class PeerConnectFailedNoticePacket : IClientBoundPacket {
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

    public PeerConnectFailedNoticePacket(string targetUserId, string reason, long timestamp) {
        TargetUserId = targetUserId;
        Reason = reason;
        Timestamp = timestamp;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PeerConnectFailedNoticePacket;
}