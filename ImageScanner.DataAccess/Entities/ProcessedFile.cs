using System.ComponentModel.DataAnnotations;

namespace ImageScanner.DataAccess.Entities;


/// <summary>
/// Tracks files that have been processed to avoid reprocessing.
/// </summary>
public class ProcessedFile : BaseEntity
{
    /// <summary>
    /// Gets or sets the full absolute path to the processed file.
    /// </summary>
    [Required]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SHA256 hash of the file content at the time of processing.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the file was last processed.
    /// </summary>
    public DateTime LastProcessed { get; set; }
}
