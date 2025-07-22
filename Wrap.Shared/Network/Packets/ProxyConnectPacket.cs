using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

/// <summary>
/// 代理连接请求包
/// </summary>
public sealed class ProxyConnectPacket : IPeerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new ProxyConnectPacketSerializer();

    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    public ProxyConnectPacket() { }

    public ProxyConnectPacket(string connectionId) {
        ConnectionId = connectionId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.ProxyConnectPacket;
}