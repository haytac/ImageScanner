namespace ImageScanner.Core.Interfaces;


/// <summary>
/// Defines logging levels.
/// </summary>
public enum LogLevel
{
    Info,
    Warning,
    Error,
    Success,
    Debug
}

/// <summary>
/// Interface for a logging service.
/// </summary>
public interface ILoggerService
{
    /// <summary>
    /// Logs a message with a specified log level.
    /// </summary>
    /// <param name="level">The level of the log message.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="context">Optional context information.</param>
    void Log(LogLevel level, string message, string? context = null);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void Info(string message, string? context = null);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void Warning(string message, string? context = null);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    void Error(string message, string? context = null);

    /// <summary>
    /// Logs an error message with an exception.
    /// </summary>
    void Error(Exception ex, string message, string? context = null);


    /// <summary>
    /// Logs a success message.
    /// </summary>
    void Success(string message, string? context = null);

    /// <summary>
    /// Logs a debug message (only if debug logging is enabled).
    /// </summary>
    void Debug(string message, string? context = null);

    /// <summary>
    /// Renders an exception in a formatted way.
    /// </summary>
    /// <param name="ex">The exception to render.</param>
    void RenderException(Exception ex);
}
