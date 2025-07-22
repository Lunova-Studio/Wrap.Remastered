using ConsoleInteractive;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Commands;

namespace Wrap.Remastered.Server.Commands;

/// <summary>
/// 退出命令
/// </summary>
public sealed class StopCommand : CommandBase {
    private readonly IServerCoordinator _server;

    public StopCommand(IServerCoordinator server) {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public override async Task OnExecuteAsync(string[] args) {
        ConsoleWriter.WriteLineFormatted("§e正在优雅地关闭服务器...");

        try {
            // 关闭服务器
            await _server.StopAsync();
            ConsoleWriter.WriteLineFormatted("§a服务器已成功关闭");

            // 等待一小段时间让日志输出完成
            await Task.Delay(1000);

            // 然后退出程序
            Environment.Exit(0);
        } catch (Exception ex) {
            ConsoleWriter.WriteLineFormatted($"§c启动关闭过程时发生错误: {ex.Message}");
            Environment.Exit(1);
        }
    }

    public override string GetName() => "stop";
    public override string GetUsage() => "stop";
    public override string GetDescription() => "优雅地关闭服务器";
}