using System.Collections.Concurrent;
using System.Net;
using Wrap.Shared.Network;
using Wrap.Shared.Network.Packets.Client;

namespace Wrap.Remastered.Server.Managers;

public sealed class PeerManager {
    /// <summary>
    /// 待处理的P2P连接请求 (请求者ID -> 目标ID -> 请求时间)
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, long>> _pendingRequests = new();

    /// <summary>
    /// 已建立的P2P连接 (用户ID -> 连接的用户ID列表)
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PeerConnectionInfo>> _establishedConnections = new();

    /// <summary>
    /// 用户连接信息
    /// </summary>
    private readonly ConcurrentDictionary<string, ChannelConnection> _userConnections = new();

    /// <summary>
    /// 添加用户连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="connection">用户连接</param>
    public void AddUserConnection(string userId, ChannelConnection connection) {
        _userConnections[userId] = connection;
    }

    /// <summary>
    /// 移除用户连接
    /// </summary>
    /// <param name="userId">用户ID</param>
    public void RemoveUserConnection(string userId) {
        _userConnections.TryRemove(userId, out _);

        // 清理该用户的所有P2P连接
        if (_establishedConnections.TryRemove(userId, out var connections)) {
            foreach (var targetUserId in connections.Keys) {
                // 通知对方用户连接断开
                if (_userConnections.TryGetValue(targetUserId, out var targetConnection)) {
                    var disconnectPacket = new PeerConnectFailedNoticePacket(userId, "用户已断开连接", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    _ = targetConnection.SendPacketAsync(disconnectPacket);
                }
            }
        }

        // 清理待处理的请求
        _pendingRequests.TryRemove(userId, out _);
        foreach (var requests in _pendingRequests.Values) {
            requests.TryRemove(userId, out _);
        }
    }

    /// <summary>
    /// 处理P2P连接请求
    /// </summary>
    /// <param name="requesterUserId">请求者用户ID</param>
    /// <param name="targetUserId">目标用户ID</param>
    /// <returns>是否成功发送请求</returns>
    public bool HandlePeerConnectRequest(string requesterUserId, string targetUserId) {
        // 检查目标用户是否存在
        if (!_userConnections.TryGetValue(targetUserId, out var targetConnection)) {
            return false;
        }

        // 检查是否已经存在P2P连接
        if (_establishedConnections.TryGetValue(requesterUserId, out var requesterConnections) &&
            requesterConnections.ContainsKey(targetUserId)) {
            // 已经存在连接，不再重复建立
            return false;
        }

        // 检查是否已有待处理请求
        if (!_pendingRequests.TryGetValue(requesterUserId, out var requests)) {
            requests = new ConcurrentDictionary<string, long>();
            _pendingRequests[requesterUserId] = requests;
        }

        // 检查是否已有反向的待处理请求
        if (_pendingRequests.TryGetValue(targetUserId, out var targetRequests) &&
            targetRequests.ContainsKey(requesterUserId)) {
            // 已经存在反向请求，不再重复
            return false;
        }

        // 添加请求
        requests[targetUserId] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 发送请求通知给目标用户
        var requestPacket = new PeerConnectRequestNoticePacket(
            requesterUserId,
            GetUserDisplayName(requesterUserId),
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );

        _ = targetConnection.SendPacketAsync(requestPacket);
        return true;
    }

    /// <summary>
    /// 处理P2P连接接受
    /// </summary>
    /// <param name="accepterUserId">接受者用户ID</param>
    /// <param name="requesterUserId">请求者用户ID</param>
    /// <returns>是否成功处理</returns>
    public bool HandlePeerConnectAccept(string accepterUserId, string requesterUserId) {
        // 检查请求是否存在
        if (!_pendingRequests.TryGetValue(requesterUserId, out var requests) ||
            !requests.TryGetValue(accepterUserId, out _)) {
            return false;
        }

        // 移除待处理请求
        requests.TryRemove(accepterUserId, out _);

        // 通知请求者连接被接受
        if (_userConnections.TryGetValue(requesterUserId, out var requesterConnection)) {
            var acceptPacket = new PeerConnectAcceptNoticePacket(
                accepterUserId,
                GetUserDisplayName(accepterUserId),
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );
            _ = requesterConnection.SendPacketAsync(acceptPacket);
        }

        // 交换IP信息
        ExchangeIPInfo(requesterUserId, accepterUserId);

        return true;
    }

    /// <summary>
    /// 处理P2P连接拒绝
    /// </summary>
    /// <param name="rejecterUserId">拒绝者用户ID</param>
    /// <param name="requesterUserId">请求者用户ID</param>
    /// <param name="reason">拒绝原因</param>
    /// <returns>是否成功处理</returns>
    public bool HandlePeerConnectReject(string rejecterUserId, string requesterUserId, string reason) {
        // 检查请求是否存在
        if (!_pendingRequests.TryGetValue(requesterUserId, out var requests) ||
            !requests.TryGetValue(rejecterUserId, out _)) {
            return false;
        }

        // 移除待处理请求
        requests.TryRemove(rejecterUserId, out _);

        // 通知请求者连接被拒绝
        if (_userConnections.TryGetValue(requesterUserId, out var requesterConnection)) {
            var rejectPacket = new PeerConnectRejectNoticePacket(
                rejecterUserId,
                GetUserDisplayName(rejecterUserId),
                reason,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );
            _ = requesterConnection.SendPacketAsync(rejectPacket);
        }

        return true;
    }

    /// <summary>
    /// 交换IP信息
    /// </summary>
    /// <param name="user1Id">用户1 ID</param>
    /// <param name="user2Id">用户2 ID</param>
    private void ExchangeIPInfo(string user1Id, string user2Id) {
        // 获取用户的真实IP地址和端口
        if (!_userConnections.TryGetValue(user1Id, out var user1Connection) ||
            !_userConnections.TryGetValue(user2Id, out var user2Connection)) {
            return;
        }

        // 获取用户1的连接信息
        var user1RemoteEndPoint = user1Connection.Channel.RemoteAddress as IPEndPoint;
        var user1IP = user1RemoteEndPoint!.Address.GetAddressBytes();
        var user1Port = user1RemoteEndPoint!.Port;

        // 获取用户2的连接信息
        var user2RemoteEndPoint = user2Connection.Channel.RemoteAddress as IPEndPoint;
        var user2IP = user2RemoteEndPoint!.Address.GetAddressBytes();
        var user2Port = user2RemoteEndPoint!.Port;

        // 发送IP信息给用户1
        var ipInfoPacket1 = new PeerIPInfoPacket(user2Id, user2IP, user2Port);
        _ = user1Connection.SendPacketAsync(ipInfoPacket1);

        // 发送IP信息给用户2
        var ipInfoPacket2 = new PeerIPInfoPacket(user1Id, user1IP, user1Port);
        _ = user2Connection.SendPacketAsync(ipInfoPacket2);

        // 记录连接状态
        RecordConnection(user1Id, user2Id);
    }

    /// <summary>
    /// 记录P2P连接
    /// </summary>
    /// <param name="user1Id">用户1 ID</param>
    /// <param name="user2Id">用户2 ID</param>
    private void RecordConnection(string user1Id, string user2Id) {
        var connectionInfo = new PeerConnectionInfo {
            ConnectedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Status = PeerConnectionStatus.Established
        };

        // 为用户1记录连接
        if (!_establishedConnections.TryGetValue(user1Id, out var user1Connections)) {
            user1Connections = new ConcurrentDictionary<string, PeerConnectionInfo>();
            _establishedConnections[user1Id] = user1Connections;
        }
        user1Connections[user2Id] = connectionInfo;

        // 为用户2记录连接
        if (!_establishedConnections.TryGetValue(user2Id, out var user2Connections)) {
            user2Connections = new ConcurrentDictionary<string, PeerConnectionInfo>();
            _establishedConnections[user2Id] = user2Connections;
        }
        user2Connections[user1Id] = connectionInfo;
    }

    /// <summary>
    /// 获取用户显示名称
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>显示名称</returns>
    private string GetUserDisplayName(string userId) {
        // TODO: 从用户管理器中获取用户信息
        return userId;
    }

    /// <summary>
    /// 获取用户的P2P连接状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>连接信息字典</returns>
    public Dictionary<string, PeerConnectionInfo> GetUserConnections(string userId) {
        if (_establishedConnections.TryGetValue(userId, out var connections)) {
            return connections.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        return [];
    }

    /// <summary>
    /// 获取用户的待处理请求
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>待处理请求字典</returns>
    public Dictionary<string, long> GetUserPendingRequests(string userId) {
        if (_pendingRequests.TryGetValue(userId, out var requests)) {
            return requests.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        return [];
    }
}

/// <summary>
/// P2P连接信息
/// </summary>
public class PeerConnectionInfo {
    /// <summary>
    /// 连接时间戳
    /// </summary>
    public long ConnectedAt { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public PeerConnectionStatus Status { get; set; }
}

/// <summary>
/// P2P连接状态
/// </summary>
public enum PeerConnectionStatus {
    /// <summary>
    /// 已建立
    /// </summary>
    Established,

    /// <summary>
    /// 连接中
    /// </summary>
    Connecting,

    /// <summary>
    /// 已断开
    /// </summary>
    Disconnected
}