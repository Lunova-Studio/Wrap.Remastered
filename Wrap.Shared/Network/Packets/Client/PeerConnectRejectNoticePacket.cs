using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

/// <summary>
/// P2P连接拒绝通知包
/// </summary>
public sealed class PeerConnectRejectNoticePacket : IClientBoundPacket {
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

    public PeerConnectRejectNoticePacket(string rejecterUserId, string rejecterDisplayName, string reason, long timestamp) {
        RejecterUserId = rejecterUserId;
        RejecterDisplayName = rejecterDisplayName;
        Reason = reason;
        Timestamp = timestamp;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PeerConnectRejectNoticePacket;
}
