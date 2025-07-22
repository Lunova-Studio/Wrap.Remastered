using Warp.Client.Interfaces;
using Wrap.Shared.Commands;
using Wrap.Shared.Interfaces.Command;
using Wrap.Shared.Managers;

namespace Wrap.ClientConsole.Commands;

/// <summary>
/// 客户端帮助命令
/// </summary>
public sealed class HelpCommand : CommandBase, ICommandTabCompleter {
    private readonly IClient _client;
    private readonly CommandManager _commandManager;

    public HelpCommand(IClient client, CommandManager commandManager) {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
    }

    public override string GetName() => "help";
    public override string GetUsage() => "help [命令名]";
    public override string GetDescription() => "显示帮助信息";

    public override async Task OnExecuteAsync(string[] args) {
        if (args.Length == 0)
            _commandManager.ShowHelp();
        else
            _commandManager.ShowCommandHelp(args[0]);
    }

    public IEnumerable<string> OnComplete(string[] args) {
        if (args.Length == 1)
            return _commandManager.GetAllCommands();

        return [];
    }
}