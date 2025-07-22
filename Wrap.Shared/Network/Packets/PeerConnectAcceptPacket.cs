using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

/// <summary>
/// P2P连接接受包
/// </summary>
public sealed class PeerConnectAcceptPacket : IServerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new PeerConnectAcceptPacketSerializer();

    /// <summary>
    /// 请求者用户 ID
    /// </summary>
    public string RequesterUserId { get; set; } = string.Empty;

    public PeerConnectAcceptPacket() { }

    public PeerConnectAcceptPacket(string requesterUserId) {
        RequesterUserId = requesterUserId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ServerBoundPacketType GetPacketType() => ServerBoundPacketType.PeerConnectAcceptPacket;
}