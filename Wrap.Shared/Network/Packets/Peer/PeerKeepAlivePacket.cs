using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Peer;

namespace Wrap.Shared.Network.Packets.Peer;

/// <summary>
/// P2P心跳包
/// </summary>
public sealed class PeerKeepAlivePacket : IPeerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new PeerKeepAlivePacketSerializer();

    /// <summary>
    /// 心跳值
    /// </summary>
    public int Value { get; set; }

    public PeerKeepAlivePacket() {
        Value = Random.Shared.Next();
    }

    public PeerKeepAlivePacket(int value) {
        Value = value;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.PeerKeepAlivePacket;
}