using System;
using System.Collections.Generic;
using Wrap.Remastered.Client;
using ConsoleInteractive;

namespace Wrap.Remastered.Commands.Client;

public class UserCommand : CommandBase, ICommandTabCompleter
{
    private readonly WrapClient _client;
    public UserCommand(WrapClient client) { _client = client; }
    public override string GetName() => "user";
    public override string GetDescription() => "查询用户信息";
    public override string GetUsage() => "user <用户ID>";

    public override void OnExecute(string[] args)
    {
        if (args.Length < 1)
        {
            ConsoleWriter.WriteLineFormatted("§c用法: user <用户ID>");
            return;
        }
        var userId = args[0];
        if (string.IsNullOrWhiteSpace(userId))
        {
            ConsoleWriter.WriteLineFormatted("§c用户ID不能为空");
            return;
        }
        _client.QueryUserInfo(userId);
        ConsoleWriter.WriteLineFormatted($"§a已发送用户信息查询请求: {userId}");
    }

    public IList<string> OnComplete(string[] args)
    {
        var list = new List<string>();
        if (args.Length == 1)
        {
            // 可扩展：补全在线用户ID
            if (_client.CurrentRoomInfo != null)
            {
                foreach (var user in _client.CurrentRoomInfo.Users)
                {
                    list.Add(user.UserId);
                }
            }
        }
        return list;
    }
} 