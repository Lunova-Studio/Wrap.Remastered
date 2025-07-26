using ConsoleInteractive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp.Client.Interfaces;
using Wrap.Shared.Commands;
using Wrap.Shared.Interfaces.Command;
using Wrap.Shared.Models;

namespace Wrap.ClientConsole.Commands;

public sealed class PingCommand : CommandBase, ICommandTabCompleter
{
    private readonly IClient _client;

    public PingCommand(IClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public override string GetName() => "ping";
    public override string GetDescription() => "测定对用户的延迟";
    public override string GetUsage() => "ping <peer|server> [userid]";

    public override async Task OnExecuteAsync(string[] args)
    {
        if (!_client.IsConnected)
        {
            ConsoleWriter.WriteLineFormatted("§c请先连接到服务器");
            return;
        }

        if (!_client.IsLoggedIn)
        {
            ConsoleWriter.WriteLineFormatted("§c请先登录");
            return;
        }

        if (args.Length == 0)
        {
            ConsoleWriter.WriteLineFormatted($"§c用法错误: {GetUsage()}");
            return;
        }

        if (args.Length >= 1)
        {
            string subCommand = args[0];

            if (subCommand == "server")
            {
                var pingResult = _client.Ping;
                ConsoleWriter.WriteLineFormatted($"§aPing {pingResult.Milliseconds}ms");
                return;
            }

            if (subCommand == "peer")
            {
                if (args.Length == 1)
                {
                    List<string> connections = _client.GetPeerConnections().ToList();
                    foreach (var connection in connections)
                    {
                        if (string.IsNullOrWhiteSpace(connection))
                        {
                            ConsoleWriter.WriteLineFormatted("§c没有可用的连接");
                            return;
                        }
                        var pingResult = _client.PeerConnectionManager.GetPing(connection);
                        ConsoleWriter.WriteLineFormatted($"§aPing {connection}: {pingResult.Milliseconds}ms");
                    }
                }
                else if (args.Length == 2)
                {
                    string userid = args[1];

                    var pingResult = _client.PeerConnectionManager.GetPing(userid);
                    ConsoleWriter.WriteLineFormatted($"§aPing {userid}: {pingResult.Milliseconds}ms");
                }
            }
        }
        else
        {
            ConsoleWriter.WriteLineFormatted($"§c用法错误: {GetUsage()}");
        }
    }

    public IEnumerable<string> OnComplete(string[] args)
    {
        if (args.Length == 1)
        {
            return ["server", "peer"];
        }

        return [];
    }
}
