using System;
using System.Collections.Generic;
using ConsoleInteractive;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Server.Managers;

namespace Wrap.Remastered.Commands.Server;

/// <summary>
/// 踢出用户命令
/// </summary>
public class KickCommand : CommandBase, ICommandTabCompleter
{
    private readonly IWrapServer _server;

    public KickCommand(IWrapServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public override string GetName() => "kick";

    public override string GetDescription() => "踢出指定用户";

    public override string GetUsage() => "kick <用户ID>";

    public override void OnExecute(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleWriter.WriteLineFormatted("§c用法: kick <用户ID>");
            return;
        }

        var userId = args[0];
        var success = _server.GetConnectionManager().DisconnectUser(userId);

        if (success)
        {
            ConsoleWriter.WriteLineFormatted($"§a已成功踢出用户: {userId}");
        }
        else
        {
            ConsoleWriter.WriteLineFormatted($"§c踢出用户失败: {userId} (用户可能不在线)");
        }
    }

    public IList<string> OnComplete(string[] args)
    {
        if (args.Length == 1)
        {
            var connections = _server.GetConnectionManager().GetAllUserConnections();
            return connections
                .Where(c => c.UserInfo != null)
                .Select(c => c.UserInfo!.UserId)
                .ToList();
        }

        return new List<string>();
    }
} 