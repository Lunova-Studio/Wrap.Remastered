using System;
using System.Collections.Generic;
using ConsoleInteractive;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Server.Managers;
using System.Linq;

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

    public override string GetUsage() => "kick <用户ID> [原因]";

    public override async Task OnExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleWriter.WriteLineFormatted("§c用法: kick <用户ID> [原因]");
            return;
        }

        var userId = args[0];
        var reason = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "被管理员踢出";
        var success = await _server.GetConnectionManager().DisconnectUserAsync(userId, reason);

        if (success)
        {
            ConsoleWriter.WriteLineFormatted($"§a已成功踢出用户: {userId}，原因：{reason}");
        }
        else
        {
            ConsoleWriter.WriteLineFormatted($"§c踢出用户失败: {userId} (用户可能不在线)");
        }
    }

    public IList<string> OnComplete(string[] args)
    {
        // 支持kick <房间ID> <用户ID> 或 kick <用户ID>
        var list = new List<string>();
        if (args.Length == 1)
        {
            // 优先补全在线用户ID
            var connections = _server.GetConnectionManager().GetAllUserConnections();
            list.AddRange(connections.Where(c => c.UserInfo != null).Select(c => c.UserInfo!.UserId));
            // 如果有RoomManager，补全房间ID
            if (_server.GetRoomManager() is { } roomMgr)
            {
                list.AddRange(roomMgr.GetAllRooms().Select(r => r.Id.ToString()));
            }
        }
        else if (args.Length == 2)
        {
            // 如果第一个参数是房间ID，补全该房间内用户ID
            if (int.TryParse(args[0], out var roomId) && _server.GetRoomManager() is { } roomMgr)
            {
                var room = roomMgr.GetRoom(roomId);
                if (room != null)
                {
                    list.AddRange(room.Users.Select(u => u.Value.UserId));
                }
            }
        }
        return list;
    }
} 