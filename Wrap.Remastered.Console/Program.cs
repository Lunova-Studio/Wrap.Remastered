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
            
            // 注册P2P连接事件
            client.PeerConnectRequestReceived += OnPeerConnectRequestReceived;

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
