namespace Wrap.Remastered.Network.Protocol.PeerBound;

public enum PeerBoundPacketType
{
    /// <summary>
    /// P2P心跳包
    /// </summary>
    PeerKeepAlivePacket,

    /// <summary>
    /// P2P心跳响应包
    /// </summary>
    PeerKeepAliveResponsePacket,

    /// <summary>
    /// 代理连接请求包
    /// </summary>
    ProxyConnectPacket,

    /// <summary>
    /// 代理数据包
    /// </summary>
    ProxyDataPacket,

    /// <summary>
    /// 代理断开连接包
    /// </summary>
    ProxyDisconnectPacket,

    /// <summary>
    /// 代理响应包
    /// </summary>
    ProxyResponsePacket,

    PluginMessage
}