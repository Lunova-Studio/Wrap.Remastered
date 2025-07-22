using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System.Net;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

/// <summary>
/// 登录数据包处理器
/// </summary>
public class LoginPacketHandler : BasePacketHandler {
    public LoginPacketHandler(IServerCoordinator server) : base(server) { }

    public override async Task OnHandleAsync(IChannel channel, UnsolvedPacket packet) {
        try {
            // 反序列化登录数据包
            if (LoginPacket.Serializer.Deserialize(packet.Data) is not LoginPacket loginPacket) {
                Server.Logger.LogError(new ArgumentNullException(nameof(loginPacket)),
                    "登录数据包反序列化失败，通道: {RemoteAddress}", channel.RemoteAddress);

                await SendLoginFailedResponseAsync(channel, "INVALID_PACKET", "数据包格式错误");
                return;
            }

            Server.Logger.LogInformation("收到登录请求: {DisplayName}", loginPacket.DisplayName);

            // 生成唯一的用户ID
            var userId = Guid.NewGuid().ToString();

            // 创建新的用户信息，使用服务器生成的用户ID
            var serverUserInfo = new UserInfo {
                UserId = userId,
                Name = loginPacket.Name,
                DisplayName = loginPacket.DisplayName
            };

            // 设置用户信息到连接
            Server.GetConnectionManager().SetUserInfo(channel, serverUserInfo);

            Server.Logger.LogInformation("用户登录成功: {DisplayName} (ID: {UserId})", serverUserInfo.DisplayName, serverUserInfo.UserId);

            // 发送登录成功响应
            await SendLoginSucceedResponseAsync(channel, serverUserInfo);

            // 添加到PeerManager
            Server.GetPeerManager().AddUserConnection(userId, Server.GetConnectionManager().GetChannelConnection(channel)!);
        } catch (Exception ex) {
            Server.Logger.LogError(ex, "处理登录请求时发生错误");
            await SendLoginFailedResponseAsync(channel, "SERVER_ERROR", "服务器内部错误");
        }
    }

    /// <summary>
    /// 发送登录成功响应
    /// </summary>
    private async Task SendLoginSucceedResponseAsync(IChannel channel, UserInfo userInfo) {
        try {
            var loginSucceedPacket = new LoginSucceedPacket {
                UserId = userInfo.UserId,
                Name = userInfo.Name,
                DisplayName = userInfo.DisplayName,
                LoginTime = DateTime.UtcNow,
                IPAddress = ((IPEndPoint)channel.RemoteAddress).Address.GetAddressBytes(),
                Port = ((IPEndPoint)channel.RemoteAddress).Port
            };

            await Server.GetConnectionManager().GetChannelConnection(channel)!.SendPacketAsync(loginSucceedPacket);
            Server.Logger.LogInformation("已发送登录成功响应给用户: {DisplayName}", userInfo.DisplayName);
        } catch (Exception ex) {
            Server.Logger.LogError(ex, "发送登录成功响应时发生错误");
        }
    }

    /// <summary>
    /// 发送登录失败响应
    /// </summary>
    private async Task SendLoginFailedResponseAsync(IChannel channel, string errorCode, string errorMessage) {
        try {
            var loginFailedPacket = new LoginFailedPacket {
                ErrorCode = 500,
                ErrorMessage = errorMessage,
                FailTime = DateTime.UtcNow
            };

            await Server.GetConnectionManager().GetChannelConnection(channel)!.SendPacketAsync(loginFailedPacket);
            Server.Logger.LogInformation("已发送登录失败响应: {errorCode} - {errorMessage}", errorCode, errorMessage);
        } catch (Exception ex) {
            Server.Logger.LogError(ex, "发送登录失败响应时发生错误");
        }
    }

    /// <summary>
    /// 发送断开连接包（踢出/服务器关闭等）
    /// </summary>
    public async Task SendDisconnectPacketAsync(IChannel channel, string reason) {
        try {
            var disconnectPacket = new DisconnectPacket {
                Reason = reason,
                DisconnectTime = DateTime.UtcNow
            };

            await Server.GetConnectionManager().GetChannelConnection(channel)!.SendPacketAsync(disconnectPacket);
            Server.Logger.LogInformation("已发送断开连接包: {reason}", reason);
        } catch (Exception ex) {
            Server.Logger.LogError(ex, "发送断开连接包时发生错误");
        }
    }
}