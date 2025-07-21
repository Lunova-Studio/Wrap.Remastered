using ConsoleInteractive;

namespace Wrap.Remastered.Commands;

/// <summary>
/// 命令管理器
/// </summary>
public class CommandManager
{
    private readonly List<ICommandExecuter> _executors = new();
    private readonly List<ICommandTabCompleter> _tabCompleters = new();

    /// <summary>
    /// 命令执行器列表
    /// </summary>
    public IReadOnlyList<ICommandExecuter> Executors => _executors.AsReadOnly();

    /// <summary>
    /// 命令标签页补全器列表
    /// </summary>
    public IReadOnlyList<ICommandTabCompleter> TabCompleters => _tabCompleters.AsReadOnly();

    /// <summary>
    /// 注册命令执行器
    /// </summary>
    /// <param name="executor">命令执行器</param>
    public void RegisterCommandExecuter(ICommandExecuter executor)
    {
        if (executor == null)
            throw new ArgumentNullException(nameof(executor));

        _executors.Add(executor);
    }

    /// <summary>
    /// 注册命令标签页补全器
    /// </summary>
    /// <param name="completer">命令标签页补全器</param>
    public void RegisterCommandTabCompleter(ICommandTabCompleter completer)
    {
        if (completer == null)
            throw new ArgumentNullException(nameof(completer));

        _tabCompleters.Add(completer);
    }

    /// <summary>
    /// 查找命令执行器
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <returns>命令执行器</returns>
    public ICommandExecuter? FindExecutor(string name)
    {
        return _executors.Find(x => x.GetName().Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 查找命令标签页补全器
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <returns>命令标签页补全器</returns>
    public ICommandTabCompleter? FindTabCompleter(string name)
    {
        return _tabCompleters.Find(x => x.GetName().Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取命令补全选项
    /// </summary>
    /// <param name="command">命令名称</param>
    /// <param name="args">命令参数</param>
    /// <returns>补全选项列表</returns>
    public IList<string> Complete(string command, string[] args)
    {
        var completer = FindTabCompleter(command);
        if (completer == null)
            return new List<string>();

        return completer.OnComplete(args);
    }

    /// <summary>
    /// 获取命令补全选项
    /// </summary>
    /// <param name="commandLine">命令行</param>
    /// <returns>补全选项列表</returns>
    public IList<string> Complete(string commandLine)
    {
        var trimmed = commandLine.TrimStart();
        if (string.IsNullOrEmpty(trimmed))
        {
            return new List<string>();
        }

        if (!trimmed.Contains(' '))
        {
            // 主命令补全
            return GetAllCommands().Where(c => c.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        var name = GetCommandName(commandLine);
        var args = GetCommandArgs(commandLine);
        return Complete(name, args);
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="command">命令名称</param>
    /// <param name="args">命令参数</param>
    public void Execute(string command, string[] args)
    {
        var executor = FindExecutor(command);
        if (executor == null)
        {
            ConsoleWriter.WriteLineFormatted($"§c未找到命令: {command}");
            return;
        }

        try
        {
            executor.OnExecuteAsync(args).Wait();
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLineFormatted($"§c在执行命令 {command} 时发生异常: {ex.GetType().Name} - {ex.Message}");
        }
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="commandLine">命令行</param>
    public void Execute(string commandLine)
    {
        var command = GetCommandName(commandLine);
        var args = GetCommandArgs(commandLine);
        Execute(command, args);
    }

    /// <summary>
    /// 获取所有可用命令
    /// </summary>
    /// <returns>命令列表</returns>
    public IList<string> GetAllCommands()
    {
        return _executors.Select(x => x.GetName()).ToList();
    }

    /// <summary>
    /// 显示帮助信息
    /// </summary>
    public void ShowHelp()
    {
        ConsoleWriter.WriteLineFormatted("§a=== 可用命令 ===");
        foreach (var executor in _executors)
        {
            ConsoleWriter.WriteLineFormatted($"§f{executor.GetName()} - {executor.GetDescription()}");
        }
    }

    /// <summary>
    /// 显示命令帮助
    /// </summary>
    /// <param name="commandName">命令名称</param>
    public void ShowCommandHelp(string commandName)
    {
        var executor = FindExecutor(commandName);
        if (executor == null)
        {
            ConsoleWriter.WriteLineFormatted($"§c未找到命令: {commandName}");
            return;
        }

        ConsoleWriter.WriteLineFormatted($"§a=== {executor.GetName()} 命令帮助 ===");
        ConsoleWriter.WriteLineFormatted($"§f描述: {executor.GetDescription()}");
        ConsoleWriter.WriteLineFormatted($"§f用法: {executor.GetUsage()}");
    }

    /// <summary>
    /// 获取命令名称
    /// </summary>
    /// <param name="commandLine">命令行</param>
    /// <returns>命令名称</returns>
    public static string GetCommandName(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return string.Empty;

        return commandLine.Split(' ').First();
    }

    /// <summary>
    /// 获取命令参数
    /// </summary>
    /// <param name="commandLine">命令行</param>
    /// <returns>命令参数数组</returns>
    public static string[] GetCommandArgs(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return Array.Empty<string>();

        var parts = commandLine.Split(' ').ToList();
        if (parts.Count > 0)
            parts.RemoveAt(0);

        return parts.ToArray();
    }
}