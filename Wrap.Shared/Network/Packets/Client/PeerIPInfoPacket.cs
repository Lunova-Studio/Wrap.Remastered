using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Client;

namespace Wrap.Shared.Network.Packets.Client;

/// <summary>
/// P2P IP信息交换包
/// </summary>
public sealed class PeerIPInfoPacket : IClientBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new PeerIPInfoPacketSerializer();

    /// <summary>
    /// 目标用户ID
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>
    /// 目标用户IP地址（字节数组）
    /// </summary>
    public byte[] TargetIPAddress { get; set; } = new byte[4];

    /// <summary>
    /// 目标用户端口
    /// </summary>
    public int TargetPort { get; set; }

    public PeerIPInfoPacket() { }

    public PeerIPInfoPacket(string targetUserId, byte[] targetIPAddress, int targetPort) {
        TargetUserId = targetUserId;
        TargetIPAddress = targetIPAddress;
        TargetPort = targetPort;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public ClientBoundPacketType GetPacketType() => ClientBoundPacketType.PeerIPInfoPacket;
}
