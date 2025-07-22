using ConsoleInteractive;
using Warp.Client.Interfaces;
using Wrap.Shared.Commands;
using Wrap.Shared.Interfaces.Command;

namespace Wrap.ClientConsole.Commands;

public sealed class UserCommand : CommandBase, ICommandTabCompleter {
    private readonly IClient _client;

    public UserCommand(IClient client) {
        _client = client;
    }

    public override string GetName() => "user";
    public override string GetUsage() => "user <用户ID>";
    public override string GetDescription() => "查询用户信息";

    public override async Task OnExecuteAsync(string[] args) {
        if (args.Length < 1) {
            ConsoleWriter.WriteLineFormatted("§c用法: user <用户ID>");
            return;
        }

        var userId = args[0];
        if (string.IsNullOrWhiteSpace(userId)) {
            ConsoleWriter.WriteLineFormatted("§c用户ID不能为空");
            return;
        }

        await _client.QueryUserInfoAsync(userId);
        ConsoleWriter.WriteLineFormatted($"§a已发送用户信息查询请求: {userId}");
    }

    public IEnumerable<string> OnComplete(string[] args) {
        var list = new List<string>();
        if (args.Length == 1) {
            // 可扩展：补全在线用户ID
            if (_client.CurrentRoomInfo != null)
                foreach (var user in _client.CurrentRoomInfo.Users)
                    list.Add(user.UserId);
        }

        return list;
    }
}