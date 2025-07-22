using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Serializers;

namespace Wrap.Shared.Network.Packets;

/// <summary>
/// 代理数据包
/// </summary>
public sealed class ProxyDataPacket : IPeerBoundPacket {
    public static ISerializer<IPacket> Serializer { get; } = new ProxyDataPacketSerializer();
    /// <summary>
    /// 顺序号
    /// </summary>
    public long SequenceId { get; set; }

    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// 数据
    /// </summary>
    public byte[] Data { get; set; } = [];

    /// <summary>
    /// 数据方向 (true: 客户端到服务器, false: 服务器到客户端)
    /// </summary>
    public bool IsClientToServer { get; set; }

    public ProxyDataPacket() { }

    public ProxyDataPacket(string connectionId, byte[] data, bool isClientToServer, long sequenceId = 0) {
        ConnectionId = connectionId;
        Data = data;
        IsClientToServer = isClientToServer;
        SequenceId = sequenceId;
    }

    public void OnSerialize(ref byte[] data) { }

    public ISerializer<IPacket> GetSerializer() => Serializer;
    public PeerBoundPacketType GetPacketType() => PeerBoundPacketType.ProxyDataPacket;
}
