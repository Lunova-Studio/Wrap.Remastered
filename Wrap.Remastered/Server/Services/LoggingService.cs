using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using ConsoleInteractive;
using Wrap.Remastered.Commands;
using Wrap.Remastered.Interfaces;
using Wrap.Remastered.Server.Managers;
using System.Linq;

namespace Wrap.Remastered.Server.Services;

/// <summary>
/// 日志服务类，集成 ConsoleInteractive 提供更好的控制台体验
/// </summary>
public class LoggingService : IDisposable
{
    private readonly object _lock = new();
    private IWrapServer _server;
    
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly Timer _flushTimer;
    private readonly bool _enableConsoleInteractive;
    
    private bool _disposed = false;

    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 日志条目
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public object?[]? Parameters { get; set; }

        public LogEntry(LogLevel level, string category, string message, Exception? exception = null, object?[]? parameters = null)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Category = category;
            Message = message;
            Exception = exception;
            Parameters = parameters;
        }
    }

    public LoggingService(IWrapServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _enableConsoleInteractive = !Console.IsOutputRedirected;
        
        if (_enableConsoleInteractive)
        {
            ConsoleWriter.Init();
            ConsoleReader.BeginReadThread();
            ConsoleReader.MessageReceived += OnCommandReceived;
            ConsoleReader.OnInputChange += (sender, e) =>
            {
                if (e.Text == string.Empty)
                {
                    ConsoleSuggestion.ClearSuggestions();
                    return;
                }
                IList<string>? strings = _server.GetCommandManager().Complete(e.Text.TrimStart());
                ConsoleSuggestion.ClearSuggestions();
                List<ConsoleSuggestion.Suggestion> suggestions = new();
                foreach (string s in strings)
                {
                    ConsoleSuggestion.Suggestion suggestion = new(s);
                    suggestions.Add(suggestion);
                }
                int offset = 0;
                int offset2 = e.Text.Length;
                if (e.Text.LastIndexOf(" ") != -1)
                {
                    offset = e.Text.LastIndexOf(" ") + 1;
                }
                ConsoleSuggestion.UpdateSuggestions(suggestions.ToArray(), new(offset, offset2));
            };
        }

        // 启动定时刷新日志的定时器
        _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// 初始化服务器命令
    /// </summary>
    /// <param name="server">服务器实例</param>
    public void InitializeServerCommands()
    {
        var commandManager = _server.GetCommandManager();
        // 注册服务器命令
        commandManager.RegisterCommandExecuter(new Commands.Server.HelpCommand(_server));
        commandManager.RegisterCommandExecuter(new Commands.Server.StatusCommand(_server));
        commandManager.RegisterCommandExecuter(new Commands.Server.UsersCommand(_server));
        commandManager.RegisterCommandExecuter(new Commands.Server.KickCommand(_server));
        commandManager.RegisterCommandExecuter(new Commands.Server.BroadcastCommand(_server));
        commandManager.RegisterCommandExecuter(new Commands.Server.StopCommand(_server));

        // 注册标签页补全器
        commandManager.RegisterCommandTabCompleter(new Commands.Server.HelpCommand(_server));
        commandManager.RegisterCommandTabCompleter(new Commands.Server.KickCommand(_server));
    }

    /// <summary>
    /// 记录调试日志
    /// </summary>
    public void LogDebug(string category, string message, params object?[] parameters)
    {
        Log(LogLevel.Debug, category, message, null, parameters);
    }

    /// <summary>
    /// 记录信息日志
    /// </summary>
    public void LogInfo(string category, string message, params object?[] parameters)
    {
        Log(LogLevel.Info, category, message, null, parameters);
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    public void LogWarning(string category, string message, params object?[] parameters)
    {
        Log(LogLevel.Warning, category, message, null, parameters);
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public void LogError(string category, string message, Exception? exception = null, params object?[] parameters)
    {
        Log(LogLevel.Error, category, message, exception, parameters);
    }

    /// <summary>
    /// 记录严重错误日志
    /// </summary>
    public void LogCritical(string category, string message, Exception? exception = null, params object?[] parameters)
    {
        Log(LogLevel.Critical, category, message, exception, parameters);
    }

    /// <summary>
    /// 记录网络相关日志
    /// </summary>
    public void LogNetwork(string message, params object?[] parameters)
    {
        LogInfo("Network", message, parameters);
    }

    /// <summary>
    /// 记录数据包相关日志
    /// </summary>
    public void LogPacket(string message, params object?[] parameters)
    {
        LogInfo("Packet", message, parameters);
    }

    /// <summary>
    /// 记录连接相关日志
    /// </summary>
    public void LogConnection(string message, params object?[] parameters)
    {
        LogInfo("Connection", message, parameters);
    }

    /// <summary>
    /// 记录用户相关日志
    /// </summary>
    public void LogUser(string message, params object?[] parameters)
    {
        LogInfo("User", message, parameters);
    }

    /// <summary>
    /// 记录服务器相关日志
    /// </summary>
    public void LogServer(string message, params object?[] parameters)
    {
        LogInfo("Server", message, parameters);
    }

    /// <summary>
    /// 内部日志记录方法
    /// </summary>
    private void Log(LogLevel level, string category, string message, Exception? exception, object?[]? parameters)
    {
        if (_disposed) return;

        try
        {
            // 格式化消息
            var formattedMessage = parameters?.Length > 0 ? string.Format(message, parameters) : message;
            
            // 创建日志条目
            var logEntry = new LogEntry(level, category, formattedMessage, exception, parameters);
            _logQueue.Enqueue(logEntry);

            // 立即输出到控制台
            WriteToConsole(logEntry);
        }
        catch (Exception ex)
        {
            // 如果日志记录失败，至少输出到标准控制台
            Console.WriteLine($"[ERROR] 日志记录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 输出到控制台
    /// </summary>
    private void WriteToConsole(LogEntry entry)
    {
        var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelText = GetLevelText(entry.Level);
        var levelColor = GetLevelColor(entry.Level);
        
        var logMessage = $"[{timestamp}] [{levelText}] [{entry.Category}] {entry.Message}";
        
        if (_enableConsoleInteractive)
        {
            // 使用 ConsoleInteractive 的格式化输出
            var formattedMessage = FormatMessageWithColor(logMessage, levelColor);
            ConsoleWriter.WriteLineFormatted(formattedMessage);
            
            // 如果有异常，输出异常信息
            if (entry.Exception != null)
            {
                var exceptionMessage = $"§c异常详情: {entry.Exception.Message}";
                ConsoleWriter.WriteLineFormatted(exceptionMessage);
                
                if (entry.Exception.StackTrace != null)
                {
                    var stackTrace = $"§c堆栈跟踪: {entry.Exception.StackTrace}";
                    ConsoleWriter.WriteLineFormatted(stackTrace);
                }
            }
        }
        else
        {
            // 回退到标准控制台输出
            Console.WriteLine(logMessage);
            if (entry.Exception != null)
            {
                Console.WriteLine($"异常详情: {entry.Exception.Message}");
                Console.WriteLine($"堆栈跟踪: {entry.Exception.StackTrace}");
            }
        }
    }

    /// <summary>
    /// 获取日志级别文本
    /// </summary>
    private static string GetLevelText(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => "DEBUG",
            LogLevel.Info => "INFO ",
            LogLevel.Warning => "WARN ",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT ",
            _ => "UNKN "
        };
    }

    /// <summary>
    /// 获取日志级别颜色
    /// </summary>
    private static string GetLevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => "§7", // 灰色
            LogLevel.Info => "§a",  // 绿色
            LogLevel.Warning => "§e", // 黄色
            LogLevel.Error => "§c",   // 红色
            LogLevel.Critical => "§4", // 深红色
            _ => "§f" // 白色
        };
    }

    /// <summary>
    /// 格式化带颜色的消息
    /// </summary>
    private static string FormatMessageWithColor(string message, string color)
    {
        return $"{color}{message}§r";
    }

    /// <summary>
    /// 刷新日志队列
    /// </summary>
    private void FlushLogs(object? state)
    {
        // 这里可以实现日志文件写入逻辑
        // 目前只是清空队列
        while (_logQueue.TryDequeue(out _))
        {
            // 可以在这里写入日志文件
        }
    }

    /// <summary>
    /// 处理命令输入
    /// </summary>
    private void OnCommandReceived(object? sender, string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        try
        {
            _server.GetCommandManager().Execute(command);
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLineFormatted($"§c命令执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示欢迎信息
    /// </summary>
    public void ShowWelcome()
    {
        ConsoleWriter.WriteLineFormatted("§a=== Wrap.Remastered 服务器 ===");
        ConsoleWriter.WriteLineFormatted("§f输入 'help' 查看可用命令");
        ConsoleWriter.WriteLineFormatted("§f输入 'status' 查看服务器状态");
        ConsoleWriter.WriteLineFormatted("");
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        _flushTimer?.Dispose();
        
        if (_enableConsoleInteractive)
        {
            ConsoleReader.StopReadThread();
        }
        
        GC.SuppressFinalize(this);
    }
} 