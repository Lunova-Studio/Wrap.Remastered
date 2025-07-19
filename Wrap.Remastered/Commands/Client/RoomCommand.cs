using System;
using System.Collections.Generic;
using Wrap.Remastered.Client;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Network.Protocol.ServerBound;
using ConsoleInteractive;

namespace Wrap.Remastered.Commands.Client;

public class RoomCommand : CommandBase, ICommandTabCompleter
{
    private readonly WrapClient _client;
    public RoomCommand(WrapClient client) { _client = client; }
    public override string GetName() => "room";
    public override string GetDescription() => "房间相关操作 (create/join/leave/kick/info)";
    public override string GetUsage() => "room <create|join|leave|kick|info> ...";

    public override void OnExecute(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleWriter.WriteLineFormatted("§c用法: room <create|join|leave|kick|info> ...");
            return;
        }
        var sub = args[0].ToLower();
        switch (sub)
        {
            case "create":
                if (args.Length < 2)
                {
                    ConsoleWriter.WriteLineFormatted("§c用法: room create <房间名> [最大人数]");
                    return;
                }
                var name = args[1];
                int max = 10;
                if (args.Length > 2 && int.TryParse(args[2], out var m)) max = m;
                _client.SendPacket(new RoomCreateRequestPacket(name, max));
                ConsoleWriter.WriteLineFormatted($"§a已发送创建房间请求: {name} 最大人数: {max}");
                break;
            case "join":
                if (args.Length < 2 || !int.TryParse(args[1], out var joinId))
                {
                    ConsoleWriter.WriteLineFormatted("§c用法: room join <房间ID>");
                    return;
                }
                _client.RequestJoinRoom(joinId);
                ConsoleWriter.WriteLineFormatted($"§a已发送加入房间请求: {joinId}");
                break;
            case "leave":
                if (_client.CurrentRoomInfo == null)
                {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }
                _client.SendPacket(new RoomLeavePacket(_client.CurrentRoomInfo.RoomId));
                ConsoleWriter.WriteLineFormatted($"§a已发送退出房间请求: {_client.CurrentRoomInfo.RoomId}");
                break;
            case "kick":
                if (args.Length < 2)
                {
                    ConsoleWriter.WriteLineFormatted("§c用法: room kick <用户ID>");
                    return;
                }
                if (_client.CurrentRoomInfo == null)
                {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }
                _client.KickUserFromRoom(_client.CurrentRoomInfo.RoomId, args[1]);
                ConsoleWriter.WriteLineFormatted($"§a已发送踢人请求: {args[1]}");
                break;
            case "info":
                if (args.Length < 2 || !int.TryParse(args[1], out var infoId))
                {
                    ConsoleWriter.WriteLineFormatted("§c用法: room info <房间ID>");
                    return;
                }
                _client.SendPacket(new RoomInfoQueryPacket(infoId));
                ConsoleWriter.WriteLineFormatted($"§a已发送房间信息查询请求: {infoId}");
                break;
            case "approve":
                if (args.Length < 2)
                {
                    ConsoleWriter.WriteLineFormatted("§c用法: room approve <用户ID>");
                    return;
                }
                if (_client.CurrentRoomInfo == null)
                {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }
                _client.ApproveJoinRoom(_client.CurrentRoomInfo.RoomId, args[1]);
                ConsoleWriter.WriteLineFormatted($"§a已同意用户加入: {args[1]}");
                break;
            case "reject":
                if (args.Length < 2)
                {
                    ConsoleWriter.WriteLineFormatted("§c用法: room reject <用户ID>");
                    return;
                }
                if (_client.CurrentRoomInfo == null)
                {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }
                _client.RejectJoinRoom(_client.CurrentRoomInfo.RoomId, args[1]);
                ConsoleWriter.WriteLineFormatted($"§a已拒绝用户加入: {args[1]}");
                break;
            case "transfer":
                if (args.Length < 2)
                {
                    ConsoleWriter.WriteLineFormatted("§c用法: room transfer <新房主用户ID>");
                    return;
                }
                if (_client.CurrentRoomInfo == null)
                {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }
                _client.TransferRoomOwner(_client.CurrentRoomInfo.RoomId, args[1]);
                ConsoleWriter.WriteLineFormatted($"§a已发送房主转让请求: {args[1]}");
                break;
            case "dismiss":
                if (_client.CurrentRoomInfo == null)
                {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }
                _client.DismissRoom(_client.CurrentRoomInfo.RoomId);
                ConsoleWriter.WriteLineFormatted($"§a已发送解散房间请求: {_client.CurrentRoomInfo.RoomId}");
                break;
            default:
                ConsoleWriter.WriteLineFormatted("§c未知子命令: " + sub);
                break;
        }
    }

    public IList<string> OnComplete(string[] args)
    {
        var list = new List<string>();
        if (args.Length == 1)
        {
            list.AddRange(new[] { "create", "join", "leave", "kick", "info", "approve", "reject", "transfer", "dismiss" });
        }
        else if (args.Length == 2 && _client.CurrentRoomInfo != null)
        {
            var sub = args[0].ToLower();
            if (sub == "approve")
            {
                // 优先显示待审批用户ID
                list.AddRange(_client.PendingJoinUserIds);
            }
            else if (sub is "kick" or "reject" or "transfer")
            {
                foreach (var u in _client.CurrentRoomInfo.Users)
                {
                    if (sub == "transfer" || u.UserId != _client.CurrentRoomInfo.Owner.UserId)
                        list.Add(u.UserId);
                }
            }
        }
        // 可扩展：info/join补全房间ID（如有房间列表缓存）
        return list;
    }
} 