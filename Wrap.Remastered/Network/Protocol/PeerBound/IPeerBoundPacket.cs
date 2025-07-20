using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Network.Protocol.PeerBound;

/// <summary>
/// P2P连接相关的包接口
/// </summary>
public interface IPeerBoundPacket : IPacket
{
    /// <summary>
    /// 获取包类型
    /// </summary>
    PeerBoundPacketType GetPacketType();
    
    /// <summary>
    /// P2P数据包序列化器字典
    /// </summary>
    public static Dictionary<PeerBoundPacketType, ISerializer<IPacket>> Serializers = new()
    {
        { PeerBoundPacketType.PeerKeepAlivePacket, new PeerKeepAlivePacketSerializer() }
    };
} 