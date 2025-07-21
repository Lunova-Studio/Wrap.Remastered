using ConsoleInteractive;
using Wrap.Remastered.Client;
using Wrap.Remastered.Helpers;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Console;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // 初始化控制台交互
            ConsoleWriter.Init();
            ConsoleReader.BeginReadThread();

            ConsoleWriter.WriteLineFormatted("§a=== Wrap.Remastered 客户端 ===");
            IUPnPService? service = await UPnPHelper.LookUpUPnPDeviceAsync(TimeSpan.FromSeconds(30));
            ConsoleWriter.WriteLineFormatted("§f输入 'help' 查看可用命令");
            ConsoleWriter.WriteLineFormatted("§f输入 'connect' 连接到服务器");
            ConsoleWriter.WriteLineFormatted("");

            // 创建客户端
            var client = new WrapClient
            {
                UPnPService = service
            };
            var commandManager = new ClientCommandManager(client);

            // 注册客户端事件
            client.LoggedIn += OnClientLoggedIn;
            client.Connected += OnClientConnected;
            client.Disconnected += OnClientDisconnected;

            // 注册WrapClient业务事件，分别输出日志
            client.RoomInfoReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§a[房间] 加入房间: {packet.RoomName} (ID: {packet.RoomId})，房主: {packet.Owner.DisplayName}，成员数: {packet.Users.Count}/{packet.MaxUsers}");
            };
            client.RoomOwnerChanged += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§e[房间] 房主已变更，房间ID: {packet.RoomId}，新房主UserId: {packet.NewOwnerUserId}");
            };
            client.RoomDismissed += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§c[房间] 房间已解散，房间ID: {packet.RoomId}");
            };
            client.RoomInfoQueryResultReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§b[房间] 查询结果：房间ID: {packet.RoomId}，名称: {packet.RoomName}，房主: {packet.OwnerUserId}，人数: {packet.UserCount}/{packet.MaxUsers}");
            };
            client.RoomJoinRequestNoticeReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§a[房间] 有用户申请加入房间，房间ID: {packet.RoomId}，申请者UserId: {packet.ApplicantUserId}");
            };
            client.RoomJoinResultReceived += (sender, packet) =>
            {
                if (!packet.Success && packet.Message.Contains("踢出"))
                {
                    ConsoleWriter.WriteLineFormatted($"§c[房间] 你被房主踢出了房间 (ID: {packet.RoomId})");
                }
                else
                {
                    ConsoleWriter.WriteLineFormatted($"§a[房间] 申请加入房间{packet.RoomId}结果: {(packet.Success ? "成功" : "失败")}，消息: {packet.Message}");
                }
            };
            client.RoomKickResultReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§c[房间] 你被房主踢出了房间 (ID: {packet.RoomId})");
            };
            client.RoomChatMessageReceived += (sender, packet) =>
            {
                var time = packet.Timestamp.ToLocalTime().ToString("HH:mm:ss");
                ConsoleWriter.WriteLineFormatted($"§b[{time}] §a{packet.SenderDisplayName}§f: {packet.Message}");
            };
            client.UserInfoResultReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§a用户信息: UserId={packet.UserInfo.UserId}, Name={packet.UserInfo.Name}, DisplayName={packet.UserInfo.DisplayName}");
            };
            client.KeepAliveReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§b[心跳] 收到心跳包，Value={packet.Value}");
            };
            client.PeerConnectRequestReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§a[P2P] 收到来自 {packet.RequesterDisplayName} 的P2P连接请求");
            };
            client.PeerConnectAcceptReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§a[P2P] {packet.AccepterDisplayName} 接受了你的P2P连接请求");
            };
            client.PeerConnectRejectReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§c[P2P] {packet.RejecterDisplayName} 拒绝了你的P2P连接请求，原因: {packet.Reason}");
            };
            client.PeerIPInfoReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§b[P2P] 收到 {packet.TargetUserId} 的IP信息，准备建立P2P连接");
            };
            client.PeerConnectSuccessReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§a[P2P] 与 {packet.TargetUserId} 的P2P连接建立成功");
            };
            client.PeerConnectFailedReceived += (sender, packet) =>
            {
                ConsoleWriter.WriteLineFormatted($"§c[P2P] 与 {packet.TargetUserId} 的P2P连接失败，原因: {packet.Reason}");
            };
            client.PeerDataSent += (sender, e) =>
            {

            };
            client.PeerConnectionEstablished += (sender, userId) =>
            {
                ConsoleWriter.WriteLineFormatted($"§a[P2P] 与 {userId} 的P2P连接建立成功（事件）");
            };
            client.PeerConnectionFailed += (sender, e) =>
            {
                ConsoleWriter.WriteLineFormatted($"§c[P2P] 与 {e.targetUserId} 的P2P连接失败，原因: {e.reason}");
            };
            // 注册WrapClient新增的专用日志事件
            client.NatTypeDetectedEvent += (sender, natType) =>
            {
                ConsoleWriter.WriteLineFormatted($"§b[NAT] NAT类型检测完成: {natType}");
            };
            client.ProxyManagerInitialized += (sender, msg) =>
            {
                ConsoleWriter.WriteLineFormatted($"§a[代理] 房主代理管理器已初始化");
            };
            client.ProxyForwardingDisabled += (sender, e) =>
            {
                ConsoleWriter.WriteLineFormatted($"§e[代理] 代理转发功能已禁用");
            };
            client.ServerMessageReceived += (sender, msg) =>
            {
                ConsoleWriter.WriteLineFormatted($"§a[服务器消息] §e {msg}");

            // 注册命令输入处理（支持Tab补全）
            ConsoleReader.MessageReceived += (sender, command) =>
            {
                commandManager.ExecuteCommand(command);
            };

            ConsoleReader.OnInputChange += (sender, e) =>
            {
                if (e.Text == string.Empty)
                {
                    ConsoleSuggestion.ClearSuggestions();
                    return;
                }
                IList<string>? strings = commandManager.Complete(e.Text.TrimStart());
                ConsoleSuggestion.ClearSuggestions();
                List<ConsoleSuggestion.Suggestion> suggestions = new();
                foreach (string s in strings)
                {
                    ConsoleSuggestion.Suggestion suggestion = new(s);
                    suggestions.Add(suggestion);
                }
                int offset = 0;
                int offset2 = e.Text.Length;
                if (e.Text.LastIndexOf(" ") != -1)
                {
                    offset = e.Text.LastIndexOf(" ") + 1;
                }
                ConsoleSuggestion.UpdateSuggestions(suggestions.ToArray(), new(offset, offset2));
            };

            // 保持程序运行
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLineFormatted($"§c客户端启动失败: {ex.Message}");
            System.Console.WriteLine("按任意键退出...");
            System.Console.ReadKey();
        }
    }

    /// <summary>
    /// 客户端连接事件处理
    /// </summary>
    private static void OnClientConnected(object? sender, EventArgs e)
    {
        ConsoleWriter.WriteLineFormatted("§a客户端已连接到服务器");
    }

    /// <summary>
    /// 客户端断开事件处理
    /// </summary>
    private static void OnClientDisconnected(object? sender, string reason)
    {
        ConsoleWriter.WriteLineFormatted($"§c客户端已断开连接 原因 {reason}");

        Environment.Exit(0);
    }

    /// <summary>
    /// 客户端登录成功事件处理
    /// </summary>
    private static void OnClientLoggedIn(object? sender, UserInfo userInfo)
    {
        ConsoleWriter.WriteLineFormatted($"§a登录成功！服务器分配的用户ID: {userInfo.UserId}");
        ConsoleWriter.WriteLineFormatted($"§f用户名: {userInfo.DisplayName}");
    }

    /// <summary>
    /// P2P连接请求事件处理
    /// </summary>
    private static void OnPeerConnectRequestReceived(object? sender, PeerConnectRequestNoticePacket packet)
    {
        if (sender is WrapClient client)
        {
            // 自动同意P2P连接请求
            ConsoleWriter.WriteLineFormatted($"§a[P2P] 自动同意来自 {packet.RequesterDisplayName} 的P2P连接请求");
            client.SendPacketAsync(new PeerConnectAcceptPacket(packet.RequesterUserId));
        }
    }
}
