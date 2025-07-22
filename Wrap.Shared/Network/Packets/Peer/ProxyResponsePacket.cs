using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers.Peer;

namespace Wrap.Shared.Network.Packets.Peer;

/// <summary>
/// 代理响应包
/// </summary>
public sealed class ProxyResponsePacket : IPeerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new ProxyResponsePacketSerializer();

    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    public ProxyResponsePacket() { }

    public ProxyResponsePacket(string connectionId, bool success, string errorMessage = "") {
        ConnectionId = connectionId;
        Success = success;
        ErrorMessage = errorMessage;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.ProxyResponsePacket;
}