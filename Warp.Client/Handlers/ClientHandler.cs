using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System.Net;
using Wrap.Shared.Enums;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Packets;
using Wrap.Shared.Network.Packets.Client;
using Wrap.Shared.Network.Packets.Server;

namespace Warp.Client.Handlers;

/// <summary>
/// 客户端处理器
/// </summary>
public class ClientHandler : ChannelHandlerAdapter {
    private readonly Client _client;

    public ClientHandler(Client client) {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// 通道激活时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    public override void ChannelActive(IChannelHandlerContext context) {
        var channel = context.Channel;
        var remoteAddress = channel.RemoteAddress;

        base.ChannelActive(context);
    }

    /// <summary>
    /// 通道非激活时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    public override void ChannelInactive(IChannelHandlerContext context) {
        var channel = context.Channel;
        var remoteAddress = channel.RemoteAddress;

        base.ChannelInactive(context);
    }

    /// <summary>
    /// 读取数据时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    /// <param name="message">消息</param>
    public override void ChannelRead(IChannelHandlerContext context, object message) {
        try {
            if (message is IByteBuffer buffer) {
                // 检查缓冲区是否有足够的数据（至少4字节用于数据包类型）
                if (buffer.ReadableBytes < 4) {
                    return; // 数据不完整，等待更多数据
                }

                int packetType = buffer.ReadIntLE();

                var data = new byte[buffer.ReadableBytes];
                buffer.ReadBytes(data);

                var unsolvedPacket = new UnsolvedPacket(packetType, data);
                _client.OnDataReceivedAsync(unsolvedPacket);

                // 尝试解析为具体的客户端数据包
                _ = TryParseClientBoundPacketAsync(packetType, data);
            }
        } catch (Exception) { } finally {
            if (message is IByteBuffer buffer) {
                buffer.Release();
            }
        }
    }

    /// <summary>
    /// 尝试解析客户端数据包
    /// </summary>
    /// <param name="packetType">数据包类型</param>
    /// <param name="data">数据</param>
    private async Task TryParseClientBoundPacketAsync(int packetType, byte[] data) {
        try {
            if (IClientBoundPacket.Serializers.TryGetValue((ClientBoundPacketType)packetType, out var serializer)) {
                var packet = serializer.Deserialize(data);
                if (packet != null && packet is IClientBoundPacket clientPacket) {
                    await _client.OnPacketReceivedAsync(clientPacket);

                    // 处理登录成功响应
                    if (packet is LoginSucceedPacket loginSucceed) {
                        _client.RemoteIP = new IPEndPoint(new IPAddress(loginSucceed.IPAddress), loginSucceed.Port);
                        _client!.UPnPService?.AddPortMappingAsync(_client!.RemoteIP.Port, SocketProtocol.TCP, ((IPEndPoint)_client._clientChannel!.LocalAddress).Port, "WrapClient");
                        var userInfo = new UserInfo {
                            UserId = loginSucceed.UserId,
                            Name = loginSucceed.Name,
                            DisplayName = loginSucceed.DisplayName
                        };

                        _client.OnLoginSuccess(userInfo);
                    }
                    // 处理断开连接包
                    else if (packet is DisconnectPacket disconnectPacket) {
                        await _client.OnDisconnectPacketReceivedAsync(disconnectPacket);
                    }
                    // 处理房间信息包
                    else if (packet is RoomInfoPacket roomInfoPacket) {
                        await _client.OnRoomInfoReceivedAsync(roomInfoPacket);
                    }
                    // 处理房主变更通知
                    else if (packet is RoomOwnerChangedPacket ownerChangedPacket) {
                        await _client.OnRoomOwnerChangedAsync(ownerChangedPacket);
                    }
                    // 处理房间解散通知
                    else if (packet is RoomDismissedPacket dismissedPacket) {
                        await _client.OnRoomDismissedAsync(dismissedPacket);
                    }
                    // 处理房间信息查询结果
                    else if (packet is RoomInfoQueryResultPacket infoQueryResultPacket) {
                        await _client.OnRoomInfoQueryResultAsync(infoQueryResultPacket);
                    }
                    // 处理房间申请通知
                    else if (packet is RoomJoinRequestNoticePacket joinRequestNoticePacket) {
                        await _client.OnRoomJoinRequestNoticeAsync(joinRequestNoticePacket);
                    }
                    // 处理房间申请结果
                    else if (packet is RoomJoinResultPacket joinResultPacket) {
                        await _client.OnRoomJoinResultAsync(joinResultPacket);
                    }
                    // 处理房间聊天消息
                    else if (packet is RoomChatMessagePacket chatMsgPacket) {
                        await _client.OnRoomChatMessageReceivedAsync(chatMsgPacket);
                    }
                    // 处理用户信息查询结果
                    else if (packet is UserInfoResultPacket userInfoResultPacket) {
                        await _client.OnUserInfoResultReceivedAsync(userInfoResultPacket);
                    }
                    // 处理KeepAlive包
                    else if (packet is KeepAlivePacket keepAlivePacket) {
                        await _client.OnKeepAliveReceivedAsync(keepAlivePacket);
                    }
                    // 处理P2P连接请求通知
                    else if (packet is PeerConnectRequestNoticePacket peerConnectRequestPacket) {
                        await _client.OnPeerConnectRequestReceivedAsync(peerConnectRequestPacket);
                    }
                    // 处理P2P连接接受通知
                    else if (packet is PeerConnectAcceptNoticePacket peerConnectAcceptPacket) {
                        await _client.OnPeerConnectAcceptReceivedAsync(peerConnectAcceptPacket);
                    }
                    // 处理P2P连接拒绝通知
                    else if (packet is PeerConnectRejectNoticePacket peerConnectRejectPacket) {
                        await _client.OnPeerConnectRejectReceivedAsync(peerConnectRejectPacket);
                    }
                    // 处理P2P IP信息包
                    else if (packet is PeerIPInfoPacket peerIPInfoPacket) {
                        await _client.OnPeerIPInfoReceivedAsync(peerIPInfoPacket);
                    }
                    // 处理P2P连接成功包
                    else if (packet is PeerConnectSuccessPacket peerConnectSuccessPacket) {
                        await _client.OnPeerConnectSuccessReceivedAsync(peerConnectSuccessPacket);
                    }
                    // 处理P2P连接失败包
                    else if (packet is PeerConnectFailedNoticePacket peerConnectFailedPacket) {
                        await _client.OnPeerConnectFailedReceivedAsync(peerConnectFailedPacket);
                    }
                    // 处理插件消息包
                    else if (packet is PluginMessagePacket pluginMessagePacket) {
                        await _client.OnPluginMessageReceivedAsync(pluginMessagePacket);
                    }
                    // 处理服务器消息包
                    else if (packet is ServerMessagePacket serverMessagePacket) {
                        await _client.OnServerMessageReceivedAsync(serverMessagePacket);
                    }
                    else if (packet is PingInfoPacket pingInfoPacket)
                    {
                        await _client.OnPingInfoReceivedAsync(pingInfoPacket);
                    }
                }
            }
        } catch (Exception) {
            // 忽略解析错误
        }
    }

    /// <summary>
    /// 异常发生时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    /// <param name="exception">异常</param>
    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) {
        _ = context.CloseAsync();
    }
}