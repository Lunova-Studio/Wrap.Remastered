using ConsoleInteractive;
using Wrap.Remastered.Client;
using Wrap.Remastered.Server.Services;
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
            ConsoleWriter.WriteLineFormatted("§f输入 'help' 查看可用命令");
            ConsoleWriter.WriteLineFormatted("§f输入 'connect' 连接到服务器");
            ConsoleWriter.WriteLineFormatted("");

            // 创建客户端
            var client = new WrapClient();
            var commandManager = new ClientCommandManager(client);

            // 注册客户端事件
            client.LoggedIn += OnClientLoggedIn;
            client.Connected += OnClientConnected;
            client.Disconnected += OnClientDisconnected;

            // 注册命令输入处理
            ConsoleReader.MessageReceived += (sender, command) =>
            {
                commandManager.ExecuteCommand(command);
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
    private static void OnClientDisconnected(object? sender, EventArgs e)
    {
        ConsoleWriter.WriteLineFormatted("§c客户端已断开连接");
    }

    /// <summary>
    /// 客户端登录成功事件处理
    /// </summary>
    private static void OnClientLoggedIn(object? sender, UserInfo userInfo)
    {
        ConsoleWriter.WriteLineFormatted($"§a登录成功！服务器分配的用户ID: {userInfo.UserId}");
        ConsoleWriter.WriteLineFormatted($"§f用户名: {userInfo.DisplayName}");
    }
}
