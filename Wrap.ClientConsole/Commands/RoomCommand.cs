using ConsoleInteractive;
using Warp.Client.Interfaces;
using Wrap.Shared.Commands;
using Wrap.Shared.Interfaces.Command;
using Wrap.Shared.Network.Packets.Server;

namespace Wrap.ClientConsole.Commands;

public sealed class RoomCommand : CommandBase, ICommandTabCompleter {
    private readonly IClient _client;
    private readonly string[] _subCommands = ["create", "join", "leave", "kick", "info", "approve", "reject", "transfer", "dismiss", "list", "chat"];

    public RoomCommand(IClient client) {
        _client = client;
    }

    public override string GetName() => "room";
    public override string GetUsage() => $"room <{string.Join("|", _subCommands)}> ...";
    public override string GetDescription() => "房间相关操作 (create/join/leave/kick/info)";

    public override async Task OnExecuteAsync(string[] args) {
        if (args.Length == 0) {
            ConsoleWriter.WriteLineFormatted($"§c用法: room <{string.Join("|", _subCommands)}> ...");
            return;
        }

        var sub = args[0].ToLower();
        switch (sub) {
            case "create":
                if (args.Length < 2) {
                    ConsoleWriter.WriteLineFormatted("§c用法: room create <房间名> [最大人数]");
                    return;
                }

                var name = args[1];
                int max = 10;
                if (args.Length > 2 && int.TryParse(args[2], out var m)) max = m;
                await _client.SendPacketAsync(new RoomCreateRequestPacket(name, max));
                ConsoleWriter.WriteLineFormatted($"§a已发送创建房间请求: {name} 最大人数: {max}");
                break;
            case "join":
                if (args.Length < 2 || !int.TryParse(args[1], out var joinId)) {
                    ConsoleWriter.WriteLineFormatted("§c用法: room join <房间ID>");
                    return;
                }

                await _client.RequestJoinRoomAsync(joinId);
                ConsoleWriter.WriteLineFormatted($"§a已发送加入房间请求: {joinId}");
                break;
            case "leave":
                if (_client.CurrentRoomInfo == null) {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }

                await _client.SendPacketAsync(new RoomLeavePacket(_client.CurrentRoomInfo.RoomId));
                ConsoleWriter.WriteLineFormatted($"§a已发送退出房间请求: {_client.CurrentRoomInfo.RoomId}");
                break;
            case "kick":
                if (args.Length < 2) {
                    ConsoleWriter.WriteLineFormatted("§c用法: room kick <用户ID>");
                    return;
                }

                if (_client.CurrentRoomInfo == null) {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }

                if (_client.UserId != _client.CurrentRoomInfo.Owner.UserId) {
                    ConsoleWriter.WriteLineFormatted("§c只有房主才能踢人");
                    return;
                }

                await _client.KickUserFromRoomAsync(_client.CurrentRoomInfo.RoomId, args[1]);
                ConsoleWriter.WriteLineFormatted($"§a已发送踢人请求: {args[1]}");
                break;
            case "info":
                if (args.Length < 2 || !int.TryParse(args[1], out var infoId)) {
                    ConsoleWriter.WriteLineFormatted("§c用法: room info <房间ID>");
                    return;
                }

                await _client.SendPacketAsync(new RoomInfoQueryPacket(infoId));
                ConsoleWriter.WriteLineFormatted($"§a已发送房间信息查询请求: {infoId}");
                break;
            case "approve":
                if (args.Length < 2) {
                    ConsoleWriter.WriteLineFormatted("§c用法: room approve <用户ID>");
                    return;
                }

                if (_client.CurrentRoomInfo == null) {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }

                if (_client.UserId != _client.CurrentRoomInfo.Owner.UserId) {
                    ConsoleWriter.WriteLineFormatted("§c只有房主才能审批入群请求");
                    return;
                }

                await _client.ApproveJoinRoomAsync(_client.CurrentRoomInfo.RoomId, args[1]);
                ConsoleWriter.WriteLineFormatted($"§a已同意用户加入: {args[1]}");
                break;
            case "reject":
                if (args.Length < 2) {
                    ConsoleWriter.WriteLineFormatted("§c用法: room reject <用户ID>");
                    return;
                }

                if (_client.CurrentRoomInfo == null) {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }

                if (_client.UserId != _client.CurrentRoomInfo.Owner.UserId) {
                    ConsoleWriter.WriteLineFormatted("§c只有房主才能审批入群请求");
                    return;
                }

                await _client.RejectJoinRoomAsync(_client.CurrentRoomInfo.RoomId, args[1]);
                ConsoleWriter.WriteLineFormatted($"§a已拒绝用户加入: {args[1]}");
                break;
            case "transfer":
                if (args.Length < 2) {
                    ConsoleWriter.WriteLineFormatted("§c用法: room transfer <新房主用户ID>");
                    return;
                }

                if (_client.CurrentRoomInfo == null) {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }

                if (_client.UserId != _client.CurrentRoomInfo.Owner.UserId) {
                    ConsoleWriter.WriteLineFormatted("§c只有房主才能转让房主");
                    return;
                }

                await _client.TransferRoomOwnerAsync(_client.CurrentRoomInfo.RoomId, args[1]);
                ConsoleWriter.WriteLineFormatted($"§a已发送房主转让请求: {args[1]}");
                break;
            case "dismiss":
                if (_client.CurrentRoomInfo == null) {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }

                if (_client.UserId != _client.CurrentRoomInfo.Owner.UserId) {
                    ConsoleWriter.WriteLineFormatted("§c只有房主才能解散房间");
                    return;
                }

                await _client.DismissRoomAsync(_client.CurrentRoomInfo.RoomId);
                ConsoleWriter.WriteLineFormatted($"§a已发送解散房间请求: {_client.CurrentRoomInfo.RoomId}");
                break;
            case "list":
                if (_client.CurrentRoomInfo == null) {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }

                ConsoleWriter.WriteLineFormatted($"§a房间成员列表 (房间ID: {_client.CurrentRoomInfo.RoomId})");
                foreach (var user in _client.CurrentRoomInfo.Users) {
                    var ownerMark = user.UserId == _client.CurrentRoomInfo.Owner.UserId ? " §e[房主]" : "";
                    ConsoleWriter.WriteLineFormatted($"§f{user.DisplayName}{ownerMark}");
                }

                break;
            case "chat":
                if (_client.CurrentRoomInfo == null) {
                    ConsoleWriter.WriteLineFormatted("§c你当前不在任何房间");
                    return;
                }

                if (args.Length < 2) {
                    ConsoleWriter.WriteLineFormatted("§c用法: room chat <消息>");
                    return;
                }

                var msg = string.Join(" ", args.Skip(1));
                if (string.IsNullOrWhiteSpace(msg)) {
                    ConsoleWriter.WriteLineFormatted("§c消息不能为空");
                    return;
                }

                await _client.SendPacketAsync(new RoomChatPacket(_client.CurrentRoomInfo.RoomId, msg));
                break;
            default:
                ConsoleWriter.WriteLineFormatted("§c未知子命令: " + sub);
                break;
        }
    }

    public IEnumerable<string> OnComplete(string[] args) {
        var list = new List<string>();
        if (args.Length == 1) {
            list.AddRange(_subCommands);
            list = list.Where(x => x.StartsWith(args[0])).ToList();
        } else if (args.Length == 2 && _client.CurrentRoomInfo != null) {
            var sub = args[0].ToLower();
            if (sub == "approve") {
                // 优先显示待审批用户ID
                list.AddRange(_client.PendingJoinUserIds);
            } else if (sub is "kick" or "reject" or "transfer") {
                foreach (var u in _client.CurrentRoomInfo.Users) {
                    if (sub == "transfer" || u.UserId != _client.CurrentRoomInfo.Owner.UserId)
                        list.Add(u.UserId);
                }
            }
        }

        // 可扩展：info/join补全房间ID（如有房间列表缓存）
        return list;
    }
}