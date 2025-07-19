using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Schemas;

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
                // 检查缓冲区是否有足够的数据（至少4字节用于数据包类型）
                if (buffer.ReadableBytes < 4)
                {
                    return; // 数据不完整，等待更多数据
                }

                int packetType = buffer.ReadIntLE();
                
                var data = new byte[buffer.ReadableBytes];
                buffer.ReadBytes(data);

                var unsolvedPacket = new UnsolvedPacket(packetType, data);
                _client.OnDataReceived(unsolvedPacket);

                // 尝试解析为具体的客户端数据包
                TryParseClientBoundPacket(packetType, data);
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
    /// 尝试解析客户端数据包
    /// </summary>
    /// <param name="packetType">数据包类型</param>
    /// <param name="data">数据</param>
    private void TryParseClientBoundPacket(int packetType, byte[] data)
    {
        try
        {
            if (IClientBoundPacket.Serializers.TryGetValue((ClientBoundPacketType)packetType, out var serializer))
            {
                var packet = serializer.Deserialize(data) as IClientBoundPacket;
                if (packet != null)
                {
                    _client.OnPacketReceived(packet);

                    // 处理登录成功响应
                    if (packet is LoginSucceedPacket loginSucceed)
                    {
                        var userInfo = new UserInfo
                        {
                            UserId = loginSucceed.UserId,
                            Name = loginSucceed.Name,
                            DisplayName = loginSucceed.DisplayName
                        };
                        _client.OnLoginSuccess(userInfo);
                    }
                    // 处理断开连接包
                    else if (packet is DisconnectPacket disconnectPacket)
                    {
                        _client.OnDisconnectPacketReceived(disconnectPacket);
                    }
                }
            }
        }
        catch (Exception)
        {
            // 忽略解析错误
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

