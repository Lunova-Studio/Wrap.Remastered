using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Server.Services;
using Wrap.Remastered.Server.Configuration;

namespace Wrap.Remastered.Server;

class Program
{
    static async Task Main(string[] args)
    {
        // 创建服务器配置，启用IPv4专用模式
        var config = new ServerConfiguration
        {
            Port = 10270,
            BossThreads = 1,
            WorkerThreads = 4,
            MaxConnections = 1000,
            IPv4Only = true
        };

        IWrapServer server = new WrapServer(config);
        LoggingService loggingService = server.GetLoggingService();

        try
        {
            // 启动服务器
            loggingService.LogServer("正在启动 Wrap.Remastered 服务器...");

            await server.StartAsync();

            loggingService.LogServer("服务器启动成功！");
            loggingService.LogServer("等待客户端连接...");

            // 保持程序运行
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            loggingService.LogCritical("Server", "服务器启动失败", ex);
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
