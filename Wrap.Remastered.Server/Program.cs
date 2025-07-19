using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Server.Services;

namespace Wrap.Remastered.Server;

class Program
{
    static async Task Main(string[] args)
    {

        IWrapServer server = new WrapServer(port: 10270, bossThreads: 1, workerThreads: 4, maxConnections: 1000);
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
