namespace ImageScanner.Core;

/// <summary>
/// Defines constant string literals used throughout the application.
/// </summary>
public static class AppConstants
{
    public const string AppName = "ImageScanner";
    public const string DefaultDbFileName = "image_scanner.db";
    public const string AppSettingsFileName = "appsettings.json";
    public const string DefaultLogFileName = "image_scanner.log"; // For persistent file logging
    public const int DefaultBatchSize = 100;
}
