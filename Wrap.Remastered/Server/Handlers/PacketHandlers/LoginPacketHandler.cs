using DotNetty.Transport.Channels;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

/// <summary>
/// 登录数据包处理器
/// </summary>
public class LoginPacketHandler : BasePacketHandler
{

    public LoginPacketHandler(IWrapServer server) : base(server)
    {

    }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet)
    {
        try
        {
            // 反序列化登录数据包
            var loginPacket = LoginPacket.Serializer.Deserialize(packet.Data) as LoginPacket;
            if (loginPacket == null)
            {
                Server.GetLoggingService().LogError("Packet", "登录数据包反序列化失败", null, "通道: {0}", channel.RemoteAddress);
                await SendLoginFailedResponse(channel, "INVALID_PACKET", "数据包格式错误");
                return;
            }

            Server.GetLoggingService().LogUser("收到登录请求: {0}", loginPacket.DisplayName);

            // 生成唯一的用户ID
            var userId = Guid.NewGuid().ToString();

            // 创建新的用户信息，使用服务器生成的用户ID
            var serverUserInfo = new UserInfo
            {
                UserId = userId,
                Name = loginPacket.Name,
                DisplayName = loginPacket.DisplayName
            };

            // 设置用户信息到连接
            Server.GetConnectionManager().SetUserInfo(channel, serverUserInfo);

            Server.GetLoggingService().LogUser("用户登录成功: {0} (ID: {1})", serverUserInfo.DisplayName, serverUserInfo.UserId);

            // 发送登录成功响应
            await SendLoginSucceedResponse(channel, serverUserInfo);
        }
        catch (Exception ex)
        {
            Server.GetLoggingService().LogError("User", "处理登录请求时发生错误", ex);
            await SendLoginFailedResponse(channel, "SERVER_ERROR", "服务器内部错误");
        }
    }

    /// <summary>
    /// 发送登录成功响应
    /// </summary>
    private async Task SendLoginSucceedResponse(IChannel channel, UserInfo userInfo)
    {
        try
        {
            var loginSucceedPacket = new LoginSucceedPacket
            {
                UserId = userInfo.UserId,
                Name = userInfo.Name,
                DisplayName = userInfo.DisplayName,
                LoginTime = DateTime.UtcNow
            };

            await SendPacketAsync(channel, loginSucceedPacket);
            Server.GetLoggingService().LogUser("已发送登录成功响应给用户: {0}", userInfo.DisplayName);
        }
        catch (Exception ex)
        {
            Server.GetLoggingService().LogError("User", "发送登录成功响应时发生错误", ex);
        }
    }

    /// <summary>
    /// 发送登录失败响应
    /// </summary>
    private async Task SendLoginFailedResponse(IChannel channel, string errorCode, string errorMessage)
    {
        try
        {
            var loginFailedPacket = new LoginFailedPacket
            {
                ErrorCode = 500,
                ErrorMessage = errorMessage,
                FailTime = DateTime.UtcNow
            };

            await SendPacketAsync(channel, loginFailedPacket);
            Server.GetLoggingService().LogUser("已发送登录失败响应: {0} - {1}", errorCode, errorMessage);
        }
        catch (Exception ex)
        {
            Server.GetLoggingService().LogError("User", "发送登录失败响应时发生错误", ex);
        }
    }

    /// <summary>
    /// 发送断开连接包（踢出/服务器关闭等）
    /// </summary>
    public async Task SendDisconnectPacket(IChannel channel, string reason)
    {
        try
        {
            var disconnectPacket = new DisconnectPacket
            {
                Reason = reason,
                DisconnectTime = DateTime.UtcNow
            };
            await SendPacketAsync(channel, disconnectPacket);
            Server.GetLoggingService().LogUser("已发送断开连接包: {0}", reason);
        }
        catch (Exception ex)
        {
            Server.GetLoggingService().LogError("User", "发送断开连接包时发生错误", ex);
        }
    }

    private async Task SendPacketAsync(IChannel channel, IClientBoundPacket packet)
    {
        var serializer = packet.GetSerializer();
        var data = serializer.Serialize(packet);
        packet.OnSerialize(ref data);
        var packetData = new byte[4 + data.Length];
        BitConverter.GetBytes((int)packet.GetPacketType()).CopyTo(packetData, 0);
        data.CopyTo(packetData, 4);
        var buffer = DotNetty.Buffers.Unpooled.WrappedBuffer(packetData);
        await channel.WriteAndFlushAsync(buffer);
    }
}