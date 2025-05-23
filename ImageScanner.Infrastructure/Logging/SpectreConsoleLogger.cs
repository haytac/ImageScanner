using ImageScanner.Core.Interfaces;
using Spectre.Console;
using LogLevel = ImageScanner.Core.Interfaces.LogLevel; // Alias to avoid conflict

namespace ImageScanner.Infrastructure.Logging;

/// <summary>
/// Implements <see cref="ILoggerService"/> using Spectre.Console for rich console output.
/// </summary>
public class SpectreConsoleLogger : ILoggerService
{
    private readonly object _lock = new object(); // Spectre Console is not entirely thread-safe for all operations.

    /// <inheritdoc />
    public void Log(LogLevel level, string message, string? context = null)
    {
        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var color = level switch
            {
                LogLevel.Info => "deepskyblue1",
                LogLevel.Warning => "yellow",
                LogLevel.Error => "red",
                LogLevel.Success => "green",
                LogLevel.Debug => "grey",
                _ => "white"
            };

            var contextMarkup = string.IsNullOrEmpty(context) ? "" : $" [[{context}]]";
            AnsiConsole.MarkupLine($"[[[{color}]{level.ToString().ToUpper()}[/]]] {timestamp} - {Markup.Escape(message)}{contextMarkup}");
        }
    }

    /// <inheritdoc />
    public void Info(string message, string? context = null) => Log(LogLevel.Info, message, context);

    /// <inheritdoc />
    public void Warning(string message, string? context = null) => Log(LogLevel.Warning, message, context);

    /// <inheritdoc />
    public void Error(string message, string? context = null) => Log(LogLevel.Error, message, context);

    /// <inheritdoc />
    public void Error(Exception ex, string message, string? context = null)
    {
        Log(LogLevel.Error, $"{message} - Exception: {ex.Message}", context);
        // Optionally, render more details of the exception if needed for console
        // AnsiConsole.WriteException(ex); // This can be verbose
    }

    /// <inheritdoc />
    public void Success(string message, string? context = null) => Log(LogLevel.Success, message, context);

    /// <inheritdoc />
    public void Debug(string message, string? context = null)
    {
        // Add a check for a global debug flag if necessary
#if DEBUG
        Log(LogLevel.Debug, message, context);
#endif
    }

    /// <inheritdoc />
    public void RenderException(Exception ex)
    {
        lock (_lock)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes);
        }
    }
}