using System.Collections.Generic;
using ConsoleInteractive;
using Wrap.Remastered.Client;

namespace Wrap.Remastered.Commands.Client;

/// <summary>
/// 客户端帮助命令
/// </summary>
public class HelpCommand : CommandBase, ICommandTabCompleter
{
    private readonly WrapClient _client;

    public HelpCommand(WrapClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public override string GetName() => "help";

    public override string GetDescription() => "显示帮助信息";

    public override string GetUsage() => "help [命令名]";

    public override void OnExecute(string[] args)
    {
        if (args.Length == 0)
        {
            _client.GetClientCommandManager().ShowHelp();
        }
        else
        {
            _client.GetClientCommandManager().ShowCommandHelp(args[0]);
        }
    }

    public IList<string> OnComplete(string[] args)
    {
        if (args.Length == 1)
        {
            return _client.GetClientCommandManager().GetAllCommands();
        }

        return new List<string>();
    }
} 