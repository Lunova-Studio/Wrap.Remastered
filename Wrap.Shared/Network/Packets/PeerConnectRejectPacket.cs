using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

/// <summary>
/// P2P连接拒绝包
/// </summary>
public sealed class PeerConnectRejectPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectRejectPacketSerializer();

    /// <summary>
    /// 请求者用户 ID
    /// </summary>
    public string RequesterUserId { get; set; } = string.Empty;

    /// <summary>
    /// 拒绝原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    public PeerConnectRejectPacket() { }

    public PeerConnectRejectPacket(string requesterUserId, string reason) {
        RequesterUserId = requesterUserId;
        Reason = reason;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.PeerConnectRejectPacket;
}