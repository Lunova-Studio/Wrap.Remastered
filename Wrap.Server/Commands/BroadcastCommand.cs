using ConsoleInteractive;
using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Commands;
using Wrap.Shared.Interfaces.Command;
using Wrap.Shared.Network.Packets;

namespace Wrap.Remastered.Server.Commands;

/// <summary>
/// 广播命令
/// </summary>
public sealed class BroadcastCommand : CommandBase, ICommandTabCompleter {
    private readonly IServerCoordinator _server;

    public BroadcastCommand(IServerCoordinator server) {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public IEnumerable<string> OnComplete(string[] args) {
        return [];
    }

    public override async Task OnExecuteAsync(string[] args) {
        if (args.Length == 0) {
            ConsoleWriter.WriteLineFormatted("§c用法: broadcast <消息>");
            return;
        }

        var message = string.Join(" ", args);
        var successCount = await _server.GetConnectionManager()
            .BroadcastToAllAsync(new ServerMessagePacket(message));

        ConsoleWriter.WriteLineFormatted($"§a广播消息已发送给 {successCount} 个用户");
        ConsoleWriter.WriteLineFormatted($"§f消息内容: {message}");
    }

    public override string GetName() => "broadcast";
    public override string GetUsage() => "broadcast <消息>";
    public override string GetDescription() => "向所有用户广播消息";
}