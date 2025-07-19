using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Server.Handlers;

/// <summary>
/// 服务端处理器
/// </summary>
public class ServerHandler : ChannelHandlerAdapter
{
    private readonly IConnectionManager _connectionManager;
    private readonly IEventLoopGroup _eventLoopGroup;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionManager">连接管理器</param>
    /// <param name="eventLoopGroup">事件循环组</param>
    public ServerHandler(IConnectionManager connectionManager, IEventLoopGroup eventLoopGroup)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _eventLoopGroup = eventLoopGroup ?? throw new ArgumentNullException(nameof(eventLoopGroup));
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
        var channel = context.Channel;
        var remoteAddress = channel.RemoteAddress;
        
        Console.WriteLine($"客户端断开: {remoteAddress}");
        
        // 通知连接管理器连接断开
        _connectionManager.OnClientDisconnected(channel);
        
        base.ChannelInactive(context);
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
                var data = new byte[buffer.ReadableBytes];
                buffer.ReadBytes(data);
                
                Console.WriteLine($"接收到数据: {data.Length} 字节");
                
                // 处理接收到的数据
                ProcessReceivedData(context.Channel, data);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理数据时发生错误: {ex.Message}");
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
    /// 处理接收到的数据
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="data">数据</param>
    private void ProcessReceivedData(IChannel channel, byte[] data)
    {
        try
        {
            // 这里可以添加数据包解析和处理逻辑
            // 暂时只是记录数据长度
            Console.WriteLine($"处理来自 {channel.RemoteAddress} 的数据: {data.Length} 字节");
            
            // 更新连接活动时间
            _connectionManager.UpdateConnectionActivity(channel);
            
            // 这里可以添加数据包处理逻辑
            // 例如：解析数据包、验证用户信息、处理业务逻辑等
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理数据时发生错误: {ex.Message}");
        }
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