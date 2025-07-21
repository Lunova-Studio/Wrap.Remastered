using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System.Net;
using System.Net.Sockets;
using Wrap.Remastered.Factory;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;

namespace Wrap.Remastered.Server.Handlers;

/// <summary>
/// 服务端处理器
/// </summary>
public class ServerHandler : ChannelHandlerAdapter
{
    private readonly IWrapServer _server;
    private readonly IEventLoopGroup _eventLoopGroup;
    private readonly IPacketHandlerFactory _packetHandlerFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionManager">连接管理器</param>
    /// <param name="eventLoopGroup">事件循环组</param>
    public ServerHandler(IWrapServer server, IEventLoopGroup eventLoopGroup)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _eventLoopGroup = eventLoopGroup ?? throw new ArgumentNullException(nameof(eventLoopGroup));
        _packetHandlerFactory = new PacketHandlerFactory(_server);
    }

    /// <summary>
    /// 通道激活时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    public override void ChannelActive(IChannelHandlerContext context)
    {
        var channel = context.Channel;
        var remoteAddress = channel.RemoteAddress;

        // 验证是否为IPv4连接（如果配置启用）
        if (_server is WrapServer wrapServer && wrapServer.Configuration.IPv4Only)
        {
            if (remoteAddress is IPEndPoint remoteEndPoint)
            {
                if (remoteEndPoint.AddressFamily != AddressFamily.InterNetwork)
                {
                    _server.GetLoggingService().LogWarning("Connection", "拒绝IPv6连接: {0}", remoteAddress);
                    _ = context.CloseAsync();
                    return;
                }
            }
        }

        _server.GetLoggingService().LogConnection("客户端连接: {0}", remoteAddress);

        // 通知连接管理器有新连接
        _server.GetConnectionManager().OnClientConnected(channel);

        base.ChannelActive(context);
    }

    /// <summary>
    /// 通道非激活时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    public override void ChannelInactive(IChannelHandlerContext context)
    {
        try
        {
            var channel = context.Channel;
            var remoteAddress = channel.RemoteAddress;

            _server.GetLoggingService().LogConnection("客户端断开: {0}", remoteAddress);

            // 通知连接管理器连接断开
            _server.GetConnectionManager()?.OnClientDisconnected(channel);
        }
        catch (ObjectDisposedException)
        {
            // 连接管理器已被释放，忽略异常
        }
        catch (Exception ex)
        {
            _server.GetLoggingService().LogError("Connection", "处理客户端断开时发生错误", ex);
        }
        finally
        {
            base.ChannelInactive(context);
        }
    }

    /// <summary>
    /// 读取数据时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    /// <param name="message">消息</param>
    public override void ChannelRead(IChannelHandlerContext context, object message)
    {
        try
        {
            if (message is IByteBuffer buffer)
            {
                // 检查缓冲区是否有足够的数据（至少4字节用于数据包类型）
                if (buffer.ReadableBytes < 4)
                {
                    return; // 数据不完整，等待更多数据
                }

                // 读取数据包类型（4字节）
                int packetType = buffer.ReadIntLE();

                // 读取剩余的数据部分
                var data = new byte[buffer.ReadableBytes];
                buffer.ReadBytes(data);

                _server.GetLoggingService().LogPacket("接收到数据包: 类型={0}, 数据长度={1} 字节", packetType, data.Length);

                // 创建未解析的数据包
                var unsolvedPacket = new UnsolvedPacket(packetType, data);

                // 处理接收到的数据包
                _ = ProcessReceivedPacketAsync(context.Channel, unsolvedPacket);
            }
        }
        catch (ObjectDisposedException)
        {
            // 连接管理器已被释放，忽略异常
        }
        catch (Exception ex)
        {
            _server.GetLoggingService().LogError("Packet", "处理数据包时发生错误", ex);
        }
        finally
        {
            // 释放缓冲区
            if (message is IByteBuffer buffer)
            {
                buffer.Release();
            }
        }
    }

    /// <summary>
    /// 异常发生时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    /// <param name="exception">异常</param>
    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
    {
        _server.GetLoggingService().LogError("Connection", "通道异常", exception, "通道: {0}", context.Channel.RemoteAddress);

        // 关闭通道
        _ = context.CloseAsync();
    }

    /// <summary>
    /// 处理接收到的数据包
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    private async Task ProcessReceivedPacketAsync(IChannel channel, UnsolvedPacket packet)
    {
        try
        {
            _server.GetLoggingService().LogPacket("处理来自 {0} 的数据包: 类型={1}, 数据长度={2} 字节",
                channel.RemoteAddress, packet.PacketType, packet.Data.Length);

            // 更新连接活动时间
            _server.GetConnectionManager().UpdateConnectionActivity(channel);

            // 使用数据包处理器工厂处理数据包
            var handler = _packetHandlerFactory.GetHandler(packet.PacketType);
            if (handler != null)
            {
                await handler.OnHandleAsync(channel, packet);
            }
            else
            {
                _server.GetLoggingService().LogWarning("Packet", "未找到数据包类型 {0} 的处理器", packet.PacketType);
            }
        }
        catch (ObjectDisposedException)
        {
            // 连接管理器已被释放，忽略异常
        }
        catch (Exception ex)
        {
            _server.GetLoggingService().LogError("Packet", "处理数据包时发生错误", ex, "通道: {0}", channel.RemoteAddress);
        }
    }
}
