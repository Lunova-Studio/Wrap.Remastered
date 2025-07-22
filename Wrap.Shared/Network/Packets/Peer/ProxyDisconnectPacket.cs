using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Peer;

namespace Wrap.Shared.Network.Packets.Peer;

/// <summary>
/// 代理断开连接包
/// </summary>
public sealed class ProxyDisconnectPacket : IPeerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new ProxyDisconnectPacketSerializer();

    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// 断开原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    public ProxyDisconnectPacket() { }

    public ProxyDisconnectPacket(string connectionId, string reason = "") {
        ConnectionId = connectionId;
        Reason = reason;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.ProxyDisconnectPacket;
}
