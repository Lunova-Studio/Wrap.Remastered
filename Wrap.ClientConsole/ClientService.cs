using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Warp.Client;
using Warp.Client.Helpers;
using Warp.Client.Interfaces;
using Wrap.Shared.Models;
using Wrap.Shared.Network.Packets;

namespace Wrap.ClientConsole;

public sealed class ClientService : BackgroundService {
    private readonly Client _client;
    private readonly ILogger<ClientService> _logger;

    public ClientService(IClient client, ILogger<ClientService> logger) {
        _logger = logger;
        _client = (client as Client)!;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var service = await UPnPHelper.LookUpUPnPDeviceAsync(TimeSpan.FromSeconds(15));
        _client.UPnPService = service;

        _logger.LogInformation("开始客户端事件注册");

        _client.LoggedIn += OnClientLoggedIn;
        _client.Connected += OnClientConnected;
        _client.Disconnected += OnClientDisconnected;

        _client.RoomInfoReceived += (sender, packet) => {
            _logger.LogInformation("[Room] 加入房间: {RoomName} (ID: {RoomId})，房主: {Owner}，成员数: {Users}/{MaxUsers}",
                packet.RoomName, packet.RoomId, packet.Owner.DisplayName, packet.Users.Count, packet.MaxUsers);
        };

        _client.RoomOwnerChanged += (sender, packet) => {
            _logger.LogWarning("§e[Room] 房主已变更，房间 ID: {RoomId}，新房主 UserId: {NewOwnerUserId}",
                packet.RoomId, packet.NewOwnerUserId);
        };

        _client.RoomDismissed += (sender, packet) => {
            _logger.LogWarning("§c[Room] 房间已解散，房间 ID: {RoomId}", packet.RoomId);
        };

        _client.RoomInfoQueryResultReceived += (sender, packet) => {
            _logger.LogInformation("§b[Room] 查询结果：房间 ID: {RoomId}，名称: {RoomName}，房主: {OwnerUserId}，人数: {UserCount}/{MaxUsers}",
                packet.RoomId, packet.RoomName, packet.OwnerUserId, packet.UserCount, packet.MaxUsers);
        };

        _client.RoomJoinRequestNoticeReceived += (sender, packet) => {
            _logger.LogWarning("§a[Room] 有用户申请加入房间，房间 ID: {RoomId}，申请者 UserId: {ApplicantUserId}",
                packet.RoomId, packet.ApplicantUserId);
        };

        _client.RoomJoinResultReceived += (sender, packet) => {
            if (!packet.Success && packet.Message.Contains("踢出"))
                _logger.LogError("§c[Room] 你被房主踢出了房间 (ID: {RoomId})", packet.RoomId);
            else
                _logger.LogInformation("§a[Room] 申请加入房间 {RoomId} 结果: {Result}，消息: {Message}",
                    packet.RoomId, packet.Success ? "成功" : "失败", packet.Message);
        };

        _client.RoomKickResultReceived += (sender, packet) => {
            _logger.LogError("§c[Room] 你被房主踢出了房间 (ID: {RoomId})", packet.RoomId);
        };

        _client.RoomChatMessageReceived += (sender, packet) => {
            _logger.LogInformation("§a{SenderDisplayName}§f: {Message}", packet.SenderDisplayName, packet.Message);
        };

        _client.UserInfoResultReceived += (sender, packet) => {
            _logger.LogInformation("§a用户信息: UserId={UserId}, Name={Name}, DisplayName={DisplayName}",
                packet.UserInfo.UserId, packet.UserInfo.Name, packet.UserInfo.DisplayName);
        };

        _client.KeepAliveReceived += (sender, packet) => {
            _logger.LogDebug("§b收到心跳包，值={Value}", packet.Value);
        };

        _client.PeerConnectRequestReceived += (sender, packet) => {
            _logger.LogWarning("§a[P2P] 收到来自 {RequesterDisplayName} 的P2P连接请求",
                packet.RequesterDisplayName);
        };

        _client.PeerConnectAcceptReceived += (sender, packet) => {
            _logger.LogInformation("§a[P2P] {AccepterDisplayName} 接受了你的P2P连接请求",
                packet.AccepterDisplayName);
        };

        _client.PeerConnectRejectReceived += (sender, packet) => {
            _logger.LogError("§c[P2P] {RejecterDisplayName} 拒绝了你的P2P连接请求，原因: {Reason}",
                packet.RejecterDisplayName, packet.Reason);
        };

        _client.PeerIPInfoReceived += (sender, packet) => {
            _logger.LogDebug("§b[P2P] 收到 {TargetUserId} 的IP信息，准备建立P2P连接",
                packet.TargetUserId);
        };

        _client.PeerConnectSuccessReceived += (sender, packet) => {
            _logger.LogInformation("§a[P2P] 与 {TargetUserId} 的P2P连接建立成功", packet.TargetUserId);
        };

        _client.PeerConnectFailedReceived += (sender, packet) => {
            _logger.LogError("§c[P2P] 与 {TargetUserId} 的P2P连接失败，原因: {Reason}",
                packet.TargetUserId, packet.Reason);
        };

        _client.PeerConnectionEstablished += (sender, userId) => {
            _logger.LogInformation("§a[P2P] 与 {UserId} 的P2P连接建立成功（事件）", userId);
        };

        _client.PeerConnectionFailed += (sender, e) => {
            _logger.LogError("§c[P2P] 与 {TargetUserId} 的P2P连接失败，原因: {Reason}",
                e.targetUserId, e.reason);
        };

        // 注册Wrap_client新增的专用日志事件
        _client.NatTypeDetectedEvent += (sender, natType) => {
            _logger.LogInformation("§b[NAT] NAT类型检测完成: {NatType}", natType);
        };

        _client.ProxyManagerInitialized += (sender, msg) => {
            _logger.LogInformation("§a房主代理管理器已初始化");
        };

        _client.ProxyForwardingDisabled += (sender, e) => {
            _logger.LogInformation("§e代理转发功能已禁用");
        };

        _client.ServerMessageReceived += (sender, msg) => {
            _logger.LogInformation("§a[Server] §e {Message}", msg);
        };

        _client.PeerConnectRequestReceived += OnPeerConnectRequestReceived;

        _logger.LogInformation("客户端事件注册完毕");
    }

    /// <summary>
    /// 客户端连接事件处理
    /// </summary>
    private void OnClientConnected(object? sender, EventArgs e) {
        _logger.LogInformation("§a客户端已连接到服务器");
    }

    /// <summary>
    /// 客户端断开事件处理
    /// </summary>
    private void OnClientDisconnected(object? sender, string reason) {
        _logger.LogError("§c客户端已断开连接，原因: {Reason}", reason);
        Environment.Exit(0);
    }

    /// <summary>
    /// 客户端登录成功事件处理
    /// </summary>
    private void OnClientLoggedIn(object? sender, UserInfo userInfo) {
        _logger.LogInformation("§a客户端登录成功！服务器分配的用户ID: {UserId}, §f用户名: {DisplayName}",
            userInfo.UserId, userInfo.DisplayName);
    }

    /// <summary>
    /// P2P连接请求事件处理
    /// </summary>
    private void OnPeerConnectRequestReceived(object? sender, PeerConnectRequestNoticePacket packet) {
        if (sender is Client client) {
            // 自动同意P2P连接请求
            _logger.LogInformation("§a[P2P] 自动同意来自 {RequesterDisplayName} 的P2P连接请求", packet.RequesterDisplayName);
            _ = client.SendPacketAsync(new PeerConnectAcceptPacket(packet.RequesterUserId));
        }
    }
}