using Wrap.Remastered.Server;
using Wrap.Remastered.Server.Managers;

namespace Wrap.Remastered.Server;

class Program
{
    static async Task Main(string[] args)
    {
        System.Console.WriteLine("=== Wrap.Remastered 服务器测试 ===");

        // 创建服务器实例
        using var server = new WrapServer(port: 10270, bossThreads: 1, workerThreads: 4, maxConnections: 1000);

        // 注册事件处理器
        server.ServerStarted += OnServerStarted;
        server.ServerStopped += OnServerStopped;
        server.ClientConnected += OnClientConnected;
        server.ClientDisconnected += OnClientDisconnected;

        try
        {
            System.Console.WriteLine("正在启动服务器...");
            
            // 启动服务器
            await server.StartAsync();

            System.Console.WriteLine("服务器已启动！按任意键停止服务器...");
            System.Console.ReadKey();

            // 停止服务器
            await server.StopAsync();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"服务器运行过程中发生错误: {ex.Message}");
        }

        System.Console.WriteLine("程序结束。按任意键退出...");
        System.Console.ReadKey();
    }

    private static void OnServerStarted(object? sender, EventArgs e)
    {
        System.Console.WriteLine("服务器已启动");
    }

    private static void OnServerStopped(object? sender, EventArgs e)
    {
        System.Console.WriteLine("服务器已停止");
    }

    private static void OnClientConnected(object? sender, ChannelConnectionEventArgs e)
    {
        var connection = e.Connection;
        System.Console.WriteLine($"客户端已连接: {connection.RemoteAddress}");
        System.Console.WriteLine($"连接时间: {connection.ConnectedAt}");
    }

    private static void OnClientDisconnected(object? sender, ChannelConnectionEventArgs e)
    {
        var connection = e.Connection;
        System.Console.WriteLine($"客户端已断开: {connection.RemoteAddress}");
        System.Console.WriteLine($"连接时长: {DateTime.UtcNow - connection.ConnectedAt}");
    }
}
