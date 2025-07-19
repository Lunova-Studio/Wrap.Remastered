using System.Collections.Generic;
using ConsoleInteractive;

namespace Wrap.Remastered.Commands.Client;

/// <summary>
/// 客户端帮助命令
/// </summary>
public class HelpCommand : CommandBase, ICommandTabCompleter
{
    private readonly CommandManager _commandManager;

    public HelpCommand(CommandManager commandManager)
    {
        _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
    }

    public override string GetName() => "help";

    public override string GetDescription() => "显示帮助信息";

    public override string GetUsage() => "help [命令名]";

    public override void OnExecute(string[] args)
    {
        if (args.Length == 0)
        {
            _commandManager.ShowHelp();
        }
        else
        {
            _commandManager.ShowCommandHelp(args[0]);
        }
    }

    public IList<string> OnComplete(string[] args)
    {
        if (args.Length == 1)
        {
            return _commandManager.GetAllCommands();
        }

        return new List<string>();
    }
} 