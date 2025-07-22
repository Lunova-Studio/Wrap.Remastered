using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

/// <summary>
/// P2P连接成功包
/// </summary>
public sealed class PeerConnectSuccessPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectSuccessPacketSerializer();

    /// <summary>
    /// 连接目标用户 ID
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;

    public PeerConnectSuccessPacket() { }

    public PeerConnectSuccessPacket(string targetUserId) {
        TargetUserId = targetUserId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.PeerConnectSuccessPacket;
}