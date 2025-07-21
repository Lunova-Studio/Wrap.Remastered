using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Server.Handlers.PacketHandlers;

namespace Wrap.Remastered.Factory;

/// <summary>
/// 数据包处理器工厂
/// </summary>
public class PacketHandlerFactory : IPacketHandlerFactory
{
    private readonly IWrapServer _server;
    private readonly Dictionary<int, IPacketHandler> _handlers;
    private readonly IPacketHandler _unknownHandler;

    public PacketHandlerFactory(IWrapServer server)
    {
        _server = server;
        _unknownHandler = new UnknownPacketHandler(server);

        _handlers = new Dictionary<int, IPacketHandler>
        {
            { (int)ServerBoundPacketType.LoginPacket, new LoginPacketHandler(server) },
            { (int)ServerBoundPacketType.RoomCreateRequestPacket, new RoomCreateRequestPacketHandler(server) },
            { (int)ServerBoundPacketType.UserInfoQueryPacket, new UserInfoQueryPacketHandler(server) },
            { (int)ServerBoundPacketType.RoomJoinRequestPacket, new RoomJoinRequestPacketHandler(server) },
            { (int)ServerBoundPacketType.RoomJoinApprovePacket, new RoomJoinApprovePacketHandler(server) },
            { (int)ServerBoundPacketType.RoomLeavePacket, new RoomLeavePacketHandler(server) },
            { (int)ServerBoundPacketType.RoomInfoQueryPacket, new RoomInfoQueryPacketHandler(server) },
            { (int)ServerBoundPacketType.RoomKickPacket, new RoomKickPacketHandler(server) },
            { (int)ServerBoundPacketType.RoomJoinRejectPacket, new RoomJoinRejectPacketHandler(server) },
            { (int)ServerBoundPacketType.RoomTransferOwnerPacket, new RoomTransferOwnerPacketHandler(server) },
            { (int)ServerBoundPacketType.RoomDismissPacket, new RoomDismissPacketHandler(server) },
            { (int)ServerBoundPacketType.RoomChatPacket, new RoomChatPacketHandler(server) },
            { (int)ServerBoundPacketType.KeepAliveResponsePacket, new KeepAliveResponsePacketHandler(server) },
            { (int)ServerBoundPacketType.PeerConnectRequestPacket, new PeerConnectRequestPacketHandler(server) },
            { (int)ServerBoundPacketType.PeerConnectAcceptPacket, new PeerConnectAcceptPacketHandler(server) },
            { (int)ServerBoundPacketType.PeerConnectRejectPacket, new PeerConnectRejectPacketHandler(server) }
        };
    }

    public IPacketHandler? GetHandler(int packetType)
    {
        // 如果找到特定的处理器，返回它
        if (_handlers.TryGetValue(packetType, out var handler))
        {
            return handler;
        }

        // 否则返回未知数据包处理器
        return _unknownHandler;
    }

    /// <summary>
    /// 注册数据包处理器
    /// </summary>
    /// <param name="packetType">数据包类型</param>
    /// <param name="handler">处理器</param>
    public void RegisterHandler(int packetType, IPacketHandler handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _handlers[packetType] = handler;
    }

    /// <summary>
    /// 注销数据包处理器
    /// </summary>
    /// <param name="packetType">数据包类型</param>
    public void UnregisterHandler(int packetType)
    {
        _handlers.Remove(packetType);
    }
}
