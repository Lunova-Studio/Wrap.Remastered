using ConsoleInteractive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wrap.Remastered.Server.Commands;
using Wrap.Shared.Managers;

namespace Wrap.Remastered.Server;

public sealed class CommandRegisterService : BackgroundService {
    private readonly IConfiguration _configuration;
    private readonly CommandManager _commandManager;
    private readonly ServerCoordinator _serverCoordinator;
    private readonly ILogger<CommandRegisterService> _logger;

    public CommandRegisterService(
        IConfiguration configuration,
        CommandManager commandManager,
        ServerCoordinator serverCoordinator,
        ILogger<CommandRegisterService> logger) {
        _logger = logger;
        _configuration = configuration;
        _commandManager = commandManager;
        _serverCoordinator = serverCoordinator;
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

            _logger.LogInformation("控制台命令注册服务已启动");
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken) {
        RegisterCommands();
        return base.StartAsync(cancellationToken);
    }

    private void RegisterCommands() {
        _commandManager.RegisterCommandExecuter(new HelpCommand(_serverCoordinator));
        _commandManager.RegisterCommandExecuter(new StatusCommand(_serverCoordinator));
        _commandManager.RegisterCommandExecuter(new UsersCommand(_serverCoordinator));
        _commandManager.RegisterCommandExecuter(new KickCommand(_serverCoordinator));
        _commandManager.RegisterCommandExecuter(new BroadcastCommand(_serverCoordinator));
        _commandManager.RegisterCommandExecuter(new StopCommand(_serverCoordinator));
        _commandManager.RegisterCommandExecuter(new SendMessageCommand(_serverCoordinator));

        // 注册标签页补全器
        _commandManager.RegisterCommandTabCompleter(new HelpCommand(_serverCoordinator));
        _commandManager.RegisterCommandTabCompleter(new KickCommand(_serverCoordinator));
        _commandManager.RegisterCommandTabCompleter(new BroadcastCommand(_serverCoordinator));
        _commandManager.RegisterCommandTabCompleter(new SendMessageCommand(_serverCoordinator));
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
