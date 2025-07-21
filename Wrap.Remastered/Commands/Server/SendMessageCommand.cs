using System;
using System.Threading.Tasks;
using ConsoleInteractive;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Network.Protocol.ClientBound;
using System.Collections.Generic;
using System.Linq;

namespace Wrap.Remastered.Commands.Server;

public class SendMessageCommand : CommandBase, ICommandTabCompleter
{
    private readonly IWrapServer _server;

    public SendMessageCommand(IWrapServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public override string GetName() => "sendmsg";

    public override string GetDescription() => "向指定用户发送服务器消息";

    public override string GetUsage() => "sendmsg <UserId> <消息内容>";

    public override async Task OnExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            ConsoleWriter.WriteLineFormatted("§c用法: sendmsg <UserId> <消息内容>");
            return;
        }

        var userId = args[0];
        var message = string.Join(' ', args.Skip(1));

        var connectionManager = _server.GetConnectionManager();
        if (connectionManager.GetUserConnection(userId) == null)
        {
            ConsoleWriter.WriteLineFormatted($"§c用户 {userId} 不在线。");
            return;
        }

        var packet = new ServerMessagePacket(message);
        var success = await connectionManager.SendPacketToUserAsync(userId, packet);

        if (success)
            ConsoleWriter.WriteLineFormatted($"§a已向用户 {userId} 发送消息。");
        else
            ConsoleWriter.WriteLineFormatted($"§c消息发送失败。");
    }

    public IList<string> OnComplete(string[] args)
    {
        var list = new List<string>();
        if (args.Length == 1)
        {
            // 补全所有在线用户ID
            var connections = _server.GetConnectionManager().GetAllUserConnections();
            list.AddRange(connections.Where(c => c.UserInfo != null).Select(c => c.UserInfo!.UserId));
        }
        return list;
    }
}