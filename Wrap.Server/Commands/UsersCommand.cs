using ConsoleInteractive;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Commands;

namespace Wrap.Remastered.Server.Commands;

/// <summary>
/// 用户命令
/// </summary>
public sealed class UsersCommand : CommandBase {
    private readonly IServerCoordinator _server;

    public UsersCommand(IServerCoordinator server) {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public override async Task OnExecuteAsync(string[] args) {
        var connections = _server.GetConnectionManager().GetAllUserConnections()
            .Where(c => c.UserInfo != null && c.IsActive)
            .ToList();

        ConsoleWriter.WriteLineFormatted("§a=== 在线用户 ===");
        if (connections.Count == 0) {
            ConsoleWriter.WriteLineFormatted("§f暂无在线用户");
            return;
        }

        foreach (var connection in connections) {
            var duration = DateTime.UtcNow - connection.ConnectedAt;

            ConsoleWriter.WriteLineFormatted($"§f用户: {connection.UserInfo!.DisplayName} ({connection.UserInfo.UserId})");
            ConsoleWriter.WriteLineFormatted($"§7  地址: {connection.RemoteAddress}");
            ConsoleWriter.WriteLineFormatted($"§7  连接时长: {duration.Hours} 小时 {duration.Minutes} 分钟");
            ConsoleWriter.WriteLineFormatted("");
        }
    }

    public override string GetName() => "users";
    public override string GetUsage() => "users";
    public override string GetDescription() => "显示在线用户";
}