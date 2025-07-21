using ConsoleInteractive;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ClientBound;

namespace Wrap.Remastered.Commands.Server;

/// <summary>
/// 广播命令
/// </summary>
public class BroadcastCommand : CommandBase, ICommandTabCompleter
{
    private readonly IWrapServer _server;

    public BroadcastCommand(IWrapServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public IList<string> OnComplete(string[] args)
    {
        return new List<string>();
    }

    public override string GetName() => "broadcast";

    public override string GetDescription() => "向所有用户广播消息";

    public override string GetUsage() => "broadcast <消息>";

    public override async Task OnExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleWriter.WriteLineFormatted("§c用法: broadcast <消息>");
            return;
        }

        var message = string.Join(" ", args);

        var successCount = await _server.GetConnectionManager().BroadcastToAllAsync(new ServerMessagePacket(message));

        ConsoleWriter.WriteLineFormatted($"§a广播消息已发送给 {successCount} 个用户");
        ConsoleWriter.WriteLineFormatted($"§f消息内容: {message}");
    }
}