using DotNetty.Transport.Channels;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Server.Managers;
using Wrap.Remastered.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

public class RoomTransferOwnerPacketHandler : RoomPacketHandler
{
    public RoomTransferOwnerPacketHandler(IWrapServer server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        // 房主转移功能已取消
        var ownerConn = Server.GetConnectionManager().GetAllUserConnections().FirstOrDefault(c => c.Channel == channel);
        if (ownerConn != null)
        {
            // 发送拒绝通知
            var disconnectPacket = new DisconnectPacket("房主转移功能已取消");
            await Server.GetConnectionManager().SendPacketToUserAsync(ownerConn.UserInfo.UserId, disconnectPacket);
        }
    }
} 