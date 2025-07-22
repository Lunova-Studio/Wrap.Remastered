using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets.Server;

/// <summary>
/// P2P连接请求包
/// </summary>
public sealed class PeerConnectRequestPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectRequestPacketSerializer();

    /// <summary>
    /// 目标用户 ID
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;

    public PeerConnectRequestPacket() { }

    public PeerConnectRequestPacket(string targetUserId) {
        TargetUserId = targetUserId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.PeerConnectRequestPacket;
}
