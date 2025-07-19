using System;
using ConsoleInteractive;
using Wrap.Remastered.Commands;
using Wrap.Remastered.Commands.Client;
using Wrap.Remastered.Client;

namespace Wrap.Remastered.Client;

/// <summary>
/// 客户端命令管理器
/// </summary>
public class ClientCommandManager
{
    private readonly CommandManager _commandManager;
    private readonly WrapClient _client;

    public ClientCommandManager(WrapClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _commandManager = new CommandManager();
        
        InitializeCommands();
    }

    /// <summary>
    /// 初始化客户端命令
    /// </summary>
    private void InitializeCommands()
    {
        // 注册客户端命令
        _commandManager.RegisterCommandExecuter(new ConnectCommand(_client));
        _commandManager.RegisterCommandExecuter(new LoginCommand(_client));
        _commandManager.RegisterCommandExecuter(new DisconnectCommand(_client));
        _commandManager.RegisterCommandExecuter(new HelpCommand(_commandManager));
        _commandManager.RegisterCommandExecuter(new SetNameCommand(_client));
        _commandManager.RegisterCommandExecuter(new SetDisplayNameCommand(_client));
        _commandManager.RegisterCommandExecuter(new RoomCommand(_client));

        // 注册标签页补全器
        _commandManager.RegisterCommandTabCompleter(new ConnectCommand(_client));
        _commandManager.RegisterCommandTabCompleter(new HelpCommand(_commandManager));
        _commandManager.RegisterCommandTabCompleter(new RoomCommand(_client));
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="commandLine">命令行</param>
    public void ExecuteCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return;

        try
        {
            _commandManager.Execute(commandLine);
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLineFormatted($"§c命令执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取命令补全选项
    /// </summary>
    /// <param name="commandLine">命令行</param>
    /// <returns>补全选项列表</returns>
    public System.Collections.Generic.IList<string> Complete(string commandLine)
    {
        return _commandManager.Complete(commandLine);
    }

    /// <summary>
    /// 显示帮助信息
    /// </summary>
    public void ShowHelp()
    {
        _commandManager.ShowHelp();
    }
} 