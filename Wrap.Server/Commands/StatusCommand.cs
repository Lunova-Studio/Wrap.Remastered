using ConsoleInteractive;
using System.Diagnostics;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Commands;

namespace Wrap.Remastered.Server.Commands;

/// <summary>
/// 状态命令
/// </summary>
public class StatusCommand : CommandBase {
    private readonly IServerCoordinator _server;

    public StatusCommand(IServerCoordinator server) {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public override async Task OnExecuteAsync(string[] args) {
        using var process = Process.GetCurrentProcess();

        var uptime = DateTime.Now - process.StartTime;
        var stats = _server.GetConnectionManager()
            .GetStatistics();

        ConsoleWriter.WriteLineFormatted("§a=== 服务器状态 ===");
        ConsoleWriter.WriteLineFormatted($"§f运行时间: {uptime.Days}天 {uptime.Hours}小时 {uptime.Minutes}分钟");
        ConsoleWriter.WriteLineFormatted($"§f内存使用: {process.WorkingSet64 / 1024 / 1024} MB");
        ConsoleWriter.WriteLineFormatted($"§f总连接数: {stats.TotalConnections}");
        ConsoleWriter.WriteLineFormatted($"§f用户连接数: {stats.UserConnections}");
        ConsoleWriter.WriteLineFormatted($"§f活跃连接数: {stats.ActiveConnections}");
        ConsoleWriter.WriteLineFormatted($"§f非活跃连接数: {stats.InactiveConnections}");
    }

    public override string GetName() => "status";
    public override string GetUsage() => "status";
    public override string GetDescription() => "显示服务器状态";
}