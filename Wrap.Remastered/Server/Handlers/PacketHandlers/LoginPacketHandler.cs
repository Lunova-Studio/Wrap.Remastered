using System;
using DotNetty.Transport.Channels;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Server.Handlers;
using Wrap.Remastered.Server.Managers;

namespace Wrap.Remastered.Server.Handlers.PacketHandlers;

/// <summary>
/// 登录数据包处理器
/// </summary>
public class LoginPacketHandler : BasePacketHandler
{
    public LoginPacketHandler(IConnectionManager connectionManager) : base(connectionManager)
    {
    }

    protected override void OnHandle(IChannel channel, UnsolvedPacket packet)
    {
        // 反序列化登录数据包
        var loginPacket = LoginPacket.Serializer.Deserialize(packet.Data) as LoginPacket;
        if (loginPacket == null)
        {
            LogInfo(channel, packet, "登录数据包反序列化失败");
            return;
        }

        LogInfo(channel, packet, $"用户登录: UserId={loginPacket.UserId}, Name={loginPacket.Name}, DisplayName={loginPacket.DisplayName}");

        // 创建用户信息
        var userInfo = loginPacket.AsUserInfo();

        // 设置用户信息到连接管理器
        if (ConnectionManager is DotNettyConnectionManager connectionManager)
        {
            connectionManager.SetUserInfo(channel, userInfo);
            LogInfo(channel, packet, $"用户 {userInfo.UserId} 登录成功");
        }
    }
} 