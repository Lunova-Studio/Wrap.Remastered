using System.Collections.Generic;
using ConsoleInteractive;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Commands.Server;

/// <summary>
/// 帮助命令
/// </summary>
public class HelpCommand : CommandBase, ICommandTabCompleter
{
    private readonly IWrapServer _server;

    public HelpCommand(IWrapServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public override string GetName() => "help";

    public override string GetDescription() => "显示帮助信息";

    public override string GetUsage() => "help [命令名]";

    public override async Task OnExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            _server.GetCommandManager().ShowHelp();
        }
        else
        {
            _server.GetCommandManager().ShowCommandHelp(args[0]);
        }
    }

    public IList<string> OnComplete(string[] args)
    {
        if (args.Length == 1)
        {
            return _server.GetCommandManager().GetAllCommands();
        }

        return new List<string>();
    }
} 