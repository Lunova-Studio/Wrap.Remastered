using Wrap.Remastered.Server.Interfaces;
using Wrap.Shared.Commands;
using Wrap.Shared.Interfaces.Command;

namespace Wrap.Remastered.Server.Commands;

/// <summary>
/// 帮助命令
/// </summary>
public sealed class HelpCommand : CommandBase, ICommandTabCompleter {
    private readonly IServerCoordinator _server;

    public HelpCommand(IServerCoordinator server) {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public override async Task OnExecuteAsync(string[] args) {
        if (args.Length == 0)
            _server.GetCommandManager().ShowHelp();
        else
            _server.GetCommandManager().ShowCommandHelp(args[0]);
    }

    public IEnumerable<string> OnComplete(string[] args) {
        if (args.Length == 1) {
            return _server.GetCommandManager().GetAllCommands();
        }

        return [];
    }

    public override string GetName() => "help";
    public override string GetUsage() => "help [命令名]";
    public override string GetDescription() => "显示帮助信息";
}