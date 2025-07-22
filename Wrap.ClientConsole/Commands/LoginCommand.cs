using ConsoleInteractive;
using Warp.Client.Interfaces;
using Wrap.Shared.Commands;
using Wrap.Shared.Models;

namespace Wrap.ClientConsole.Commands;

/// <summary>
/// 登录命令
/// </summary>
public sealed class LoginCommand : CommandBase {
    private readonly IClient _client;

    public LoginCommand(IClient client) {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public override string GetName() => "login";
    public override string GetDescription() => "登录到服务器";
    public override string GetUsage() => "login <用户名> [显示名称]";

    public override async Task OnExecuteAsync(string[] args) {
        var profile = _client.Profile;
        string userName = profile.Name;
        string displayName = profile.DisplayName;

        if (args.Length >= 1)
            userName = args[0];

        if (args.Length >= 2)
            displayName = args[1];

        // 如果本地没有则提示输入
        if (string.IsNullOrWhiteSpace(userName)) {
            ConsoleWriter.WriteLineFormatted("§e请输入用户名:");
            userName = Console.ReadLine() ?? "用户";
        }

        if (string.IsNullOrWhiteSpace(displayName)) {
            ConsoleWriter.WriteLineFormatted("§e请输入显示名称:");
            displayName = Console.ReadLine() ?? userName;
        }

        // 保存到本地
        profile.Name = userName;
        profile.DisplayName = displayName;
        profile.Save();

        try {
            if (!_client.IsConnected) {
                ConsoleWriter.WriteLineFormatted("§c请先连接到服务器");
                return;
            }

            if (_client.IsLoggedIn) {
                ConsoleWriter.WriteLineFormatted("§c已经登录了");
                return;
            }

            var userInfo = new UserInfo {
                UserId = "", // 服务器会生成用户ID
                Name = userName,
                DisplayName = displayName
            };

            ConsoleWriter.WriteLineFormatted($"§a正在登录: {displayName}...");
            await _client.SendLoginPacketAsync(userInfo);
        } catch (Exception ex) {
            ConsoleWriter.WriteLineFormatted($"§c登录失败: {ex.Message}");
        }
    }
}