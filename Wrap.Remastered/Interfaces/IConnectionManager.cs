using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Server.Managers;

namespace Wrap.Remastered.Interfaces;

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
    void SetUserInfo(IChannel channel, UserInfo serverUserInfo);

    /// <summary>
    /// 更新连接活动时间
    /// </summary>
    /// <param name="channel">通道</param>
    void UpdateConnectionActivity(IChannel channel);

    IEnumerable<ChannelConnection> GetAllUserConnections();

    IEnumerable<ChannelConnection> GetAllConnections();

    Task<bool> DisconnectUserAsync(string userId, string? reason = null);

    ConnectionStatistics GetStatistics();

    Task<int> BroadcastToUsersAsync(byte[] data, string? excludeUserId = null);

    Task<bool> SendPacketToUserAsync(string userId, IClientBoundPacket packet);

    ChannelConnection? GetChannelConnection(IChannel channel);

    ChannelConnection? GetUserConnection(string userId);
    Task<int> BroadcastToAllAsync(IClientBoundPacket packet);
    Task<int> BroadcastToUsersAsync(IClientBoundPacket packet, string? excludeUserId = null);
}
