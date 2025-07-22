using ConsoleInteractive;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Wrap.Shared.Logging;

public sealed partial class Logger : ILogger {
    private readonly string _categoryName;
    private readonly LoggerConfiguration _configuration;

    public Logger(string categoryName, LoggerConfiguration configuration) {
        _categoryName = categoryName;
        _configuration = configuration;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel) {
        if (_categoryName.Contains("Microsoft") || _categoryName.Contains("System"))
            return logLevel >= LogLevel.Warning;

        return logLevel >= _configuration.MinimumLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId,
        TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var formatedMessage = Format(logLevel, message, exception!);

        ConsoleWriter.WriteLineFormatted($"§f{formatedMessage}");
    }

    private string Format(LogLevel logLevel, string message, Exception exception = null) {
        var log = _configuration.OutputTemplate;

        log = ProcessTimestampFormat(log ?? string.Empty);
        log = ProcessLevelFormat(log, logLevel);

        if (log.Contains("SourceContext"))
            log = log.Replace("{SourceContext}", _categoryName);

        if (log.Contains("Message"))
            log = log.Replace("{Message}", message.Trim());

        if (log.Contains("Exception"))
            log = log.Replace("{NewLine}", exception is null ? string.Empty : Environment.NewLine)
                .Replace("{Exception}", exception?.ToString() ?? string.Empty);

        return log;
    }

    private static string ProcessTimestampFormat(string template) {
        var timestampMatch = TemplateTimestampRegex().Match(template);

        if (timestampMatch.Success) {
            string format = timestampMatch.Groups[1].Success
                ? DateTime.Now.ToString(timestampMatch.Groups[1].Value)
                : DateTime.Now.ToString();

            string replacement = format ?? string.Empty;
            return template.Replace(timestampMatch.Value, replacement);
        } else
            return TemplateTimestampReplaceRegex().Replace(template, string.Empty);
    }

    public static string ProcessLevelFormat(string template, LogLevel levelValue) {
        var match = TemplateLevelRegex().Match(template);
        var color = levelValue switch {
            LogLevel.Debug => "§b",
            LogLevel.Information => "§a",
            LogLevel.Warning => "§e",
            LogLevel.Error => "§c",
            LogLevel.Trace => "§7",
            _ => string.Empty,
        };

        if (match.Success) {
            string format = match.Groups[1].Success
                ? match.Groups[1].Value
                : string.Empty;

            string replacement;
            if (format is "u3")
                replacement = levelValue switch {
                    LogLevel.Debug => "DBG",
                    LogLevel.Information => "INF",
                    LogLevel.Warning => "WRN",
                    LogLevel.Error => "ERR",
                    LogLevel.Trace => "TRA",
                    _ => levelValue.ToString(),
                };
            else
                replacement = levelValue.ToString();

            return template.Replace(match.Value, $"{color}{replacement}§f");
        } else
            return TemplateLevelReplaceRegex().Replace(template, string.Empty);
    }


    [GeneratedRegex(@"\{Timestamp(?::([^\}]+))?\}")]
    private static partial Regex TemplateTimestampRegex();

    [GeneratedRegex(@"\[\{Timestamp[^\}]*\}[^\]]*\]")]
    private static partial Regex TemplateTimestampReplaceRegex();

    [GeneratedRegex(@"\{Level(?::([^\}]+))?\}")]
    private static partial Regex TemplateLevelRegex();

    [GeneratedRegex(@"\{Level(?::[^\}]+)?\}")]
    private static partial Regex TemplateLevelReplaceRegex();
}

public sealed class LoggerProvider : ILoggerProvider {
    private readonly LoggerConfiguration _configuration;

    public LoggerProvider(LoggerConfiguration configuration) {
        _configuration = configuration;
    }

    public ILogger CreateLogger(string categoryName) =>
        new Logger(categoryName, _configuration);

    public void Dispose() { }
}