namespace ImageScanner.Core.Models;


/// <summary>
/// Configuration settings for the Image Scanner application.
/// </summary>
public class ImageScannerSettings
{
    /// <summary>
    /// Gets or sets the default list of image extensions to scan.
    /// </summary>
    public List<string> DefaultImageExtensions { get; set; } = new List<string> { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

    /// <summary>
    /// Gets or sets the batch size for database operations.
    /// </summary>
    public int DatabaseBatchSize { get; set; } = AppConstants.DefaultBatchSize;

    /// <summary>
    /// Gets or sets the path for the SQLite database file.
    /// </summary>
    public string DatabasePath { get; set; } = AppConstants.DefaultDbFileName;

    /// <summary>
    /// Gets or sets the path for the persistent log file.
    /// </summary>
    public string LogFilePath { get; set; } = AppConstants.DefaultLogFileName;

    /// <summary>
    /// Gets or sets a list of specific EXIF metadata fields to extract and store.
    /// </summary>
    public List<string> MetadataFieldsToExtract { get; set; } = new List<string> { "Make", "Model", "Date/Time Original", "Image Width", "Image Height", "Exposure Time", "F-Number" };

    /// <summary>
    /// Gets or sets the minimum file size in bytes to process. Files smaller than this will be skipped.
    /// Default is 0 (no minimum).
    /// </summary>
    public long MinFileSize { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum file size in bytes to process. Files larger than this will be skipped.
    /// Default is 0 (no maximum).
    /// </summary>
    public long MaxFileSize { get; set; } = 0; // 0 means no limit
}
