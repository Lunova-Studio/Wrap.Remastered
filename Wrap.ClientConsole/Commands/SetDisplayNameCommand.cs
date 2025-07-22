using ConsoleInteractive;
using Warp.Client.Interfaces;
using Wrap.Shared.Commands;

namespace Wrap.ClientConsole.Commands;

public sealed class SetDisplayNameCommand : CommandBase {
    private readonly IClient _client;

    public SetDisplayNameCommand(IClient client) {
        _client = client;
    }

    public override string GetName() => "setdisplayname";
    public override string GetDescription() => "设置本地显示名称";
    public override string GetUsage() => "setdisplayname <显示名称>";

    public override async Task OnExecuteAsync(string[] args) {
        if (args.Length < 1) {
            ConsoleWriter.WriteLineFormatted("§e用法: setdisplayname <显示名称>");
            return;
        }

        var displayName = args[0];
        _client.Profile.DisplayName = displayName;
        _client.Profile.Save();
        ConsoleWriter.WriteLineFormatted($"§a显示名称已设置为: {displayName}");
    }
}