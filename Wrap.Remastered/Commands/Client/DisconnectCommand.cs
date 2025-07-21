using System;
using ConsoleInteractive;
using Wrap.Remastered.Client;

namespace Wrap.Remastered.Commands.Client;

/// <summary>
/// 断开连接命令
/// </summary>
public class DisconnectCommand : CommandBase
{
    private readonly WrapClient _client;

    public DisconnectCommand(WrapClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public override string GetName() => "disconnect";

    public override string GetDescription() => "断开与服务器的连接";

    public override string GetUsage() => "disconnect";

    public override async Task OnExecuteAsync(string[] args)
    {
        try
        {
            ConsoleWriter.WriteLineFormatted("§e正在断开连接...");
            await Task.Run(() => _client.Dispose());
            ConsoleWriter.WriteLineFormatted("§a已断开连接");

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLineFormatted($"§c断开连接失败: {ex.Message}");
        }
    }
} 