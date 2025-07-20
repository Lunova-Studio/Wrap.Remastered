using DotNetty.Transport.Channels;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Server.Managers;
using Wrap.Remastered.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public class KeepAliveResponsePacketHandler : RoomPacketHandler
{
    public KeepAliveResponsePacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        var req = KeepAliveResponsePacket.Serializer.Deserialize(packet.Data) as KeepAliveResponsePacket;
        if (req == null) return;
        
        var connection = Server.GetConnectionManager().GetChannelConnection(channel);
        if (connection == null) return;
        
        // 验证KeepAlive响应值
        if (connection.ValidateKeepAliveResponse(req.Value))
        {
            // 验证成功，连接保持活跃
            Server.GetLoggingService().LogConnection("KeepAlive验证成功: {0}", connection.RemoteAddress);
        }
        else
        {
            // 验证失败，断开连接
            Server.GetLoggingService().LogConnection("KeepAlive验证失败，断开连接: {0}", connection.RemoteAddress);
            await Server.GetConnectionManager().DisconnectUserAsync(connection.UserId ?? "", "KeepAlive验证失败");
        }
    }
} 