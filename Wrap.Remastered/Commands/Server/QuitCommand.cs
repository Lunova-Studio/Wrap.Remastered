using System;
using ConsoleInteractive;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Commands.Server;

/// <summary>
/// 退出命令
/// </summary>
public class QuitCommand : CommandBase
{
    private readonly IWrapServer _server;

    public QuitCommand(IWrapServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public override string GetName() => "quit";

    public override string GetDescription() => "优雅地关闭服务器";

    public override string GetUsage() => "quit";

    public override void OnExecute(string[] args)
    {
        ConsoleWriter.WriteLineFormatted("§e正在优雅地关闭服务器...");
        
        try
        {
            // 异步关闭服务器
            _ = Task.Run(async () =>
            {
                try
                {
                    // 关闭服务器
                    await _server.StopAsync();
                    ConsoleWriter.WriteLineFormatted("§a服务器已成功关闭");
                    
                    // 等待一小段时间让日志输出完成
                    await Task.Delay(1000);
                    
                    // 然后退出程序
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    ConsoleWriter.WriteLineFormatted($"§c关闭服务器时发生错误: {ex.Message}");
                    Environment.Exit(1);
                }
            });
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLineFormatted($"§c启动关闭过程时发生错误: {ex.Message}");
            Environment.Exit(1);
        }
    }
} 