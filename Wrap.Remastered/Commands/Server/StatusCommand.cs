using System;
using System.Collections.Generic;
using System.Diagnostics;
using ConsoleInteractive;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Server.Managers;

namespace Wrap.Remastered.Commands.Server;

/// <summary>
/// 状态命令
/// </summary>
public class StatusCommand : CommandBase
{
    private readonly IWrapServer _server;

    public StatusCommand(IWrapServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public override string GetName() => "status";

    public override string GetDescription() => "显示服务器状态";

    public override string GetUsage() => "status";

    public override void OnExecute(string[] args)
    {
        var stats = _server.GetConnectionManager().GetStatistics();
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.Now - process.StartTime;

        ConsoleWriter.WriteLineFormatted("§a=== 服务器状态 ===");
        ConsoleWriter.WriteLineFormatted($"§f运行时间: {uptime.Days}天 {uptime.Hours}小时 {uptime.Minutes}分钟");
        ConsoleWriter.WriteLineFormatted($"§f内存使用: {process.WorkingSet64 / 1024 / 1024} MB");
        ConsoleWriter.WriteLineFormatted($"§f总连接数: {stats.TotalConnections}");
        ConsoleWriter.WriteLineFormatted($"§f用户连接数: {stats.UserConnections}");
        ConsoleWriter.WriteLineFormatted($"§f活跃连接数: {stats.ActiveConnections}");
        ConsoleWriter.WriteLineFormatted($"§f非活跃连接数: {stats.InactiveConnections}");
    }
} 