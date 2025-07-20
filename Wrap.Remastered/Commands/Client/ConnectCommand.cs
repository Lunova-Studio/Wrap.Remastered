using System;
using System.Collections.Generic;
using ConsoleInteractive;
using Wrap.Remastered.Client;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Commands.Client;

/// <summary>
/// 连接命令
/// </summary>
public class ConnectCommand : CommandBase, ICommandTabCompleter
{
    private readonly WrapClient _client;

    public ConnectCommand(WrapClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public override string GetName() => "connect";

    public override string GetDescription() => "连接到服务器";

    public override string GetUsage() => "connect [服务器地址] [端口]";

    public override void OnExecute(string[] args)
    {
        string address = "127.0.0.1";
        int port = 10270;

        if (args.Length >= 1)
        {
            address = args[0];
        }

        if (args.Length >= 2 && int.TryParse(args[1], out var parsedPort))
        {
            port = parsedPort;
        }

        // 自动读取本地用户名和显示名
        var profile = _client.Profile;
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            ConsoleWriter.WriteLineFormatted("§e请输入用户名:");
            profile.Name = System.Console.ReadLine() ?? "用户";
        }
        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            ConsoleWriter.WriteLineFormatted("§e请输入显示名称:");
            profile.DisplayName = System.Console.ReadLine() ?? profile.Name;
        }
        profile.Save();

        try
        {
            ConsoleWriter.WriteLineFormatted($"§a正在连接到 {address}:{port}...");
            
            // 注册登录成功事件
            _client.LoggedIn += OnLoggedIn;

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            _client.ConnectAsync(address, port).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

            ConsoleWriter.WriteLineFormatted($"§a连接成功！正在发送登录请求...");
            
            // 发送登录请求
            var userInfo = new UserInfo
            {
                UserId = "", // 服务器会生成用户ID
                Name = profile.Name,
                DisplayName = profile.DisplayName
            };

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            _client.SendLoginPacketAsync(userInfo).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLineFormatted($"§c连接失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 登录成功事件处理
    /// </summary>
    private void OnLoggedIn(object? sender, UserInfo userInfo)
    {
        ConsoleWriter.WriteLineFormatted($"§a登录成功！用户ID: {userInfo.UserId}");
        ConsoleWriter.WriteLineFormatted($"§f用户名: {userInfo.DisplayName}");
    }

    public IList<string> OnComplete(string[] args)
    {
        if (args.Length == 1)
        {
            return new List<string> { "127.0.0.1", "localhost", "0.0.0.0" };
        }

        if (args.Length == 2)
        {
            return new List<string> { "10270", "8080", "9000" };
        }

        return new List<string>();
    }
} 