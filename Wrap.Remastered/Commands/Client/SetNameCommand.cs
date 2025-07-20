using System;
using ConsoleInteractive;
using Wrap.Remastered.Client;

namespace Wrap.Remastered.Commands.Client;

public class SetNameCommand : CommandBase
{
    private readonly WrapClient _client;

    public SetNameCommand(WrapClient client)
    {
        _client = client;
    }

    public override string GetName() => "setname";
    public override string GetDescription() => "设置本地用户名";
    public override string GetUsage() => "setname <用户名>";

    public override async Task OnExecuteAsync(string[] args)
    {
        if (args.Length < 1)
        {
            ConsoleWriter.WriteLineFormatted("§e用法: setname <用户名>");
            return;
        }
        var name = args[0];
        _client.Profile.Name = name;
        _client.Profile.Save();
        ConsoleWriter.WriteLineFormatted($"§a用户名已设置为: {name}");
    }
} 