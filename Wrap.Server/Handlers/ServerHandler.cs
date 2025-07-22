using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Interfaces;
using Wrap.Shared.Network.Factories;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers;

/// <summary>
/// 服务端处理器
/// </summary>
public class ServerHandler : ChannelHandlerAdapter {
    private readonly IEventLoopGroup _eventLoopGroup;
    private readonly IServerCoordinator _serverCoordinator;
    private readonly IPacketHandlerFactory _packetHandlerFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionManager">连接管理器</param>
    /// <param name="eventLoopGroup">事件循环组</param>
    public ServerHandler(IServerCoordinator server, IEventLoopGroup eventLoopGroup) {
        _serverCoordinator = server ?? throw new ArgumentNullException(nameof(server));
        _eventLoopGroup = eventLoopGroup ?? throw new ArgumentNullException(nameof(eventLoopGroup));
        _packetHandlerFactory = new PacketHandlerFactory(_serverCoordinator);
    }

    /// <summary>
    /// 通道激活时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    public override void ChannelActive(IChannelHandlerContext context) {
        var channel = context.Channel;
        var remoteAddress = channel.RemoteAddress;

        // 验证是否为IPv4连接（如果配置启用）
        if (_serverCoordinator is ServerCoordinator serverCoordinator && serverCoordinator.Configuration.IsIPv4Only) {
            if (remoteAddress is IPEndPoint remoteEndPoint) {
                if (remoteEndPoint.AddressFamily != AddressFamily.InterNetwork) {
                    _serverCoordinator.Logger.LogInformation("拒绝 IPv6 连接: {remoteAddress}", remoteAddress);
                    _ = context.CloseAsync();
                    return;
                }
            }
        }

        _serverCoordinator.Logger.LogInformation("客户端连接: {remoteAddress}", remoteAddress);

        // 通知连接管理器有新连接
        _serverCoordinator.GetConnectionManager().OnClientConnected(channel);

        base.ChannelActive(context);
    }

    /// <summary>
    /// 通道非激活时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    public override void ChannelInactive(IChannelHandlerContext context) {
        try {
            var channel = context.Channel;
            var remoteAddress = channel.RemoteAddress;

            _serverCoordinator.Logger.LogInformation("客户端断开: {remoteAddress}", remoteAddress);

            // 通知连接管理器连接断开
            _serverCoordinator.GetConnectionManager()?.OnClientDisconnected(channel);
        } catch (ObjectDisposedException) {
            // 连接管理器已被释放，忽略异常
        } catch (Exception ex) {
            _serverCoordinator.Logger.LogError(ex, "处理客户端断开时发生错误");
        } finally {
            base.ChannelInactive(context);
        }
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

                // 读取数据包类型（4字节）
                int packetType = buffer.ReadIntLE();

                // 读取剩余的数据部分
                var data = new byte[buffer.ReadableBytes];
                buffer.ReadBytes(data);

                _serverCoordinator.Logger.LogInformation("接收到数据包: 类型={packetType}, 数据长度={data} 字节", packetType, data.Length);

                // 创建未解析的数据包
                var unsolvedPacket = new UnsolvedPacket(packetType, data);

                // 处理接收到的数据包
                _ = ProcessReceivedPacketAsync(context.Channel, unsolvedPacket);
            }
        } catch (ObjectDisposedException) {
            // 连接管理器已被释放，忽略异常
        } catch (Exception ex) {
            _serverCoordinator.Logger.LogError(ex, "处理数据包时发生错误");
        } finally {
            // 释放缓冲区
            if (message is IByteBuffer buffer) {
                buffer.Release();
            }
        }
    }

    /// <summary>
    /// 异常发生时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    /// <param name="exception">异常</param>
    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) {
        _serverCoordinator.Logger.LogError(exception, "通道异常,通道: {RemoteAddress}", context.Channel.RemoteAddress);

        // 关闭通道
        _ = context.CloseAsync();
    }

    /// <summary>
    /// 处理接收到的数据包
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    private async Task ProcessReceivedPacketAsync(IChannel channel, UnsolvedPacket packet) {
        try {
            _serverCoordinator.Logger.LogInformation("处理来自 {RemoteAddress} 的数据包: 类型={PacketType}, 数据长度={Data} 字节",
                channel.RemoteAddress, packet.PacketType, packet.Data.Length);

            // 更新连接活动时间
            _serverCoordinator.GetConnectionManager().UpdateConnectionActivity(channel);

            // 使用数据包处理器工厂处理数据包
            var handler = _packetHandlerFactory.GetHandler(packet.PacketType);
            if (handler != null) {
                await handler.OnHandleAsync(channel, packet);
            } else {
                _serverCoordinator.Logger.LogWarning("未找到数据包类型 {PacketType} 的处理器", packet.PacketType);
            }
        } catch (ObjectDisposedException) {
            // 连接管理器已被释放，忽略异常
        } catch (Exception ex) {
            _serverCoordinator.Logger.LogError(ex, "处理数据包时发生错误, 通道: {RemoteAddress}", channel.RemoteAddress);
        }
    }
}