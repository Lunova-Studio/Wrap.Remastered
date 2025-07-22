using ConsoleInteractive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Warp.Client.Interfaces;
using Wrap.ClientConsole.Commands;
using Wrap.Shared.Managers;

namespace Wrap.ClientConsole;

public sealed class CommandRegisterService : BackgroundService {
    private readonly IClient _client;
    private readonly IConfiguration _configuration;
    private readonly CommandManager _commandManager;
    private readonly ILogger<CommandRegisterService> _logger;

    public CommandRegisterService(
        IClient client,
        IConfiguration configuration,
        CommandManager commandManager,
        ILogger<CommandRegisterService> logger) {
        _client = client;
        _logger = logger;
        _configuration = configuration;
        _commandManager = commandManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (_configuration.GetValue<bool>("Wrap:Console:IsEnableCommand")) {
            ConsoleWriter.Init();
            ConsoleReader.BeginReadThread();
            ConsoleReader.MessageReceived += OnCommandReceived;

            ConsoleReader.OnInputChange += (sender, e) => {
                if (e.Text == string.Empty) {
                    ConsoleSuggestion.ClearSuggestions();
                    return;
                }

                var strings = _commandManager.Complete(e.Text.TrimStart());
                ConsoleSuggestion.ClearSuggestions();
                List<ConsoleSuggestion.Suggestion> suggestions = [];

                foreach (string s in strings) {
                    ConsoleSuggestion.Suggestion suggestion = new(s);
                    suggestions.Add(suggestion);
                }

                int offset = 0;
                int offset2 = e.Text.Length;
                if (e.Text.LastIndexOf(' ') != -1)
                    offset = e.Text.LastIndexOf(' ') + 1;

                ConsoleSuggestion.UpdateSuggestions([.. suggestions], new(offset, offset2));
            };
        }

        ConsoleWriter.WriteLineFormatted("§a=== Wrap.Remastered 客户端 ===");
        ConsoleWriter.WriteLineFormatted("§f开始查找 UPnP 设备 超时时间: 15s");
        ConsoleWriter.WriteLineFormatted("§f输入 'help' 查看可用命令");
        ConsoleWriter.WriteLineFormatted("§f输入 'connect' 连接到服务器");
        ConsoleWriter.WriteLine("");

        _logger.LogInformation("控制台命令注册服务已启动");
    }

    public override Task StartAsync(CancellationToken cancellationToken) {
        RegisterCommands();
        return base.StartAsync(cancellationToken);
    }

    private void RegisterCommands() {
        // 注册客户端命令
        _commandManager.RegisterCommandExecuter(new ConnectCommand(_client));
        _commandManager.RegisterCommandExecuter(new LoginCommand(_client));
        _commandManager.RegisterCommandExecuter(new DisconnectCommand(_client));
        _commandManager.RegisterCommandExecuter(new SetNameCommand(_client));
        _commandManager.RegisterCommandExecuter(new SetDisplayNameCommand(_client));
        _commandManager.RegisterCommandExecuter(new RoomCommand(_client));
        _commandManager.RegisterCommandExecuter(new UserCommand(_client));
        _commandManager.RegisterCommandExecuter(new HelpCommand(_client, _commandManager));

        // 注册标签页补全器
        _commandManager.RegisterCommandTabCompleter(new RoomCommand(_client));
        _commandManager.RegisterCommandTabCompleter(new UserCommand(_client));
        _commandManager.RegisterCommandTabCompleter(new ConnectCommand(_client));
        _commandManager.RegisterCommandTabCompleter(new HelpCommand(_client, _commandManager));
    }

    private void OnCommandReceived(object? sender, string command) {
        if (string.IsNullOrWhiteSpace(command))
            return;

        try {
            _commandManager.Execute(command);
        } catch (Exception ex) {
            ConsoleWriter.WriteLineFormatted($"§c命令执行失败: {ex.Message}");
        }
    }
}