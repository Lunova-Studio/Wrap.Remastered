using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Peer;

namespace Wrap.Shared.Network.Packets.Peer;

/// <summary>
/// P2P心跳响应包
/// </summary>
public sealed class PeerKeepAliveResponsePacket : IPeerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new PeerKeepAliveResponsePacketSerializer();

    /// <summary>
    /// 心跳值（与请求包中的值相同）
    /// </summary>
    public int Value { get; set; }

    public PeerKeepAliveResponsePacket() {
        Value = 0;
    }

    public PeerKeepAliveResponsePacket(int value) {
        Value = value;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.PeerKeepAliveResponsePacket;
}