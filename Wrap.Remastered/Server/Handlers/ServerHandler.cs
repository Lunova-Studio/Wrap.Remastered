using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Server.Handlers.PacketHandlers;

namespace Wrap.Remastered.Server.Handlers;

/// <summary>
/// 服务端处理器
/// </summary>
public class ServerHandler : ChannelHandlerAdapter
{
    private readonly IConnectionManager _connectionManager;
    private readonly IEventLoopGroup _eventLoopGroup;
    private readonly IPacketHandlerFactory _packetHandlerFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionManager">连接管理器</param>
    /// <param name="eventLoopGroup">事件循环组</param>
    public ServerHandler(IConnectionManager connectionManager, IEventLoopGroup eventLoopGroup)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _eventLoopGroup = eventLoopGroup ?? throw new ArgumentNullException(nameof(eventLoopGroup));
        _packetHandlerFactory = new PacketHandlerFactory(connectionManager);
    }

    /// <summary>
    /// 通道激活时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    public override void ChannelActive(IChannelHandlerContext context)
    {
        var channel = context.Channel;
        var remoteAddress = channel.RemoteAddress;
        
        Console.WriteLine($"客户端连接: {remoteAddress}");
        
        // 通知连接管理器有新连接
        _connectionManager.OnClientConnected(channel);
        
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
            
            Console.WriteLine($"客户端断开: {remoteAddress}");
            
            // 通知连接管理器连接断开
            _connectionManager.OnClientDisconnected(channel);
        }
        catch (ObjectDisposedException)
        {
            // 连接管理器已被释放，忽略异常
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理客户端断开时发生错误: {ex.Message}");
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
                int packetType = buffer.ReadInt();
                
                // 读取剩余的数据部分
                var data = new byte[buffer.ReadableBytes];
                buffer.ReadBytes(data);

                Console.WriteLine($"接收到数据包: 类型={packetType}, 数据长度={data.Length} 字节");

                // 创建未解析的数据包
                var unsolvedPacket = new UnsolvedPacket(packetType, data);
                
                // 处理接收到的数据包
                ProcessReceivedPacket(context.Channel, unsolvedPacket);
            }
        }
        catch (ObjectDisposedException)
        {
            // 连接管理器已被释放，忽略异常
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理数据包时发生错误: {ex.Message}");
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
        Console.WriteLine($"通道异常: {exception.Message}");

        // 关闭通道
        _ = context.CloseAsync();
    }

    /// <summary>
    /// 处理接收到的数据包
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    private void ProcessReceivedPacket(IChannel channel, UnsolvedPacket packet)
    {
        try
        {
            Console.WriteLine($"处理来自 {channel.RemoteAddress} 的数据包: 类型={packet.PacketType}, 数据长度={packet.Data.Length} 字节");
            
            // 更新连接活动时间
            _connectionManager.UpdateConnectionActivity(channel);
            
            // 使用数据包处理器工厂处理数据包
            var handler = _packetHandlerFactory.GetHandler(packet.PacketType);
            if (handler != null)
            {
                handler.Handle(channel, packet);
            }
            else
            {
                Console.WriteLine($"未找到数据包类型 {packet.PacketType} 的处理器");
            }
        }
        catch (ObjectDisposedException)
        {
            // 连接管理器已被释放，忽略异常
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理数据包时发生错误: {ex.Message}");
        }
    }
}

/// <summary>
/// 数据包处理器接口
/// </summary>
public interface IPacketHandler
{
    /// <summary>
    /// 处理数据包
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="packet">数据包</param>
    void Handle(IChannel channel, UnsolvedPacket packet);
}

/// <summary>
/// 数据包处理器工厂接口
/// </summary>
public interface IPacketHandlerFactory
{
    /// <summary>
    /// 获取数据包处理器
    /// </summary>
    /// <param name="packetType">数据包类型</param>
    /// <returns>数据包处理器</returns>
    IPacketHandler? GetHandler(int packetType);
}

/// <summary>
/// 数据包处理器工厂
/// </summary>
public class PacketHandlerFactory : IPacketHandlerFactory
{
    private readonly IConnectionManager _connectionManager;
    private readonly Dictionary<int, IPacketHandler> _handlers;
    private readonly IPacketHandler _unknownHandler;

    public PacketHandlerFactory(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
        _unknownHandler = new UnknownPacketHandler(connectionManager);
        
        _handlers = new Dictionary<int, IPacketHandler>
        {
            { (int)ServerBoundPacketType.LoginPacket, new LoginPacketHandler(connectionManager) }
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

/// <summary>
/// 连接管理器接口
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// 客户端连接时调用
    /// </summary>
    /// <param name="channel">通道</param>
    void OnClientConnected(IChannel channel);
    
    /// <summary>
    /// 客户端断开时调用
    /// </summary>
    /// <param name="channel">通道</param>
    void OnClientDisconnected(IChannel channel);
    
    /// <summary>
    /// 更新连接活动时间
    /// </summary>
    /// <param name="channel">通道</param>
    void UpdateConnectionActivity(IChannel channel);
} 