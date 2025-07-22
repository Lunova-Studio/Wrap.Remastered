using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

/// <summary>
/// P2P连接接受通知包
/// </summary>
public sealed class PeerConnectAcceptNoticePacket : IClientBoundPacket {
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

    public PeerConnectAcceptNoticePacket(string accepterUserId, string accepterDisplayName, long timestamp) {
        AccepterUserId = accepterUserId;
        AccepterDisplayName = accepterDisplayName;
        Timestamp = timestamp;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PeerConnectAcceptNoticePacket;
}