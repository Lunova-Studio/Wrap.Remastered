using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ServerBound;

namespace Wrap.Remastered.Client;

/// <summary>
/// 客户端处理器
/// </summary>
public class ClientHandler : ChannelHandlerAdapter
{
    private readonly WrapClient _client;

    public ClientHandler(WrapClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// 通道激活时调用
    /// </summary>
    /// <param name="context">通道上下文</param>
    public override void ChannelActive(IChannelHandlerContext context)
    {
        var channel = context.Channel;
        var remoteAddress = channel.RemoteAddress;

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
                var data = new byte[buffer.ReadableBytes - 1];
                int packetType = buffer.ReadInt();
                buffer.ReadBytes(data);

                _client.OnDataReceived(new UnsolvedPacket(packetType, data));
            }
        }
        catch (Exception) { }
        finally
        {
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
        _ = context.CloseAsync();
    }
}

