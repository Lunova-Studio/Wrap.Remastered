using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

/// <summary>
/// P2P连接请求通知包
/// </summary>
public class PeerConnectRequestNoticePacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectRequestNoticePacketSerializer();

    /// <summary>
    /// 请求者用户ID
    /// </summary>
    public string RequesterUserId { get; set; } = string.Empty;

    /// <summary>
    /// 请求者显示名称
    /// </summary>
    public string RequesterDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 请求时间戳
    /// </summary>
    public long Timestamp { get; set; }

    public PeerConnectRequestNoticePacket() { }

    public PeerConnectRequestNoticePacket(string requesterUserId, string requesterDisplayName, long timestamp) {
        RequesterUserId = requesterUserId;
        RequesterDisplayName = requesterDisplayName;
        Timestamp = timestamp;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PeerConnectRequestNoticePacket;
}
