using System.ComponentModel.DataAnnotations;

namespace ImageScanner.DataAccess.Entities;


/// <summary>
/// Represents detailed information about a scanned image file.
/// </summary>
public class ImageInfo : BaseEntity
{
    /// <summary>
    /// Gets or sets the name of the image file.
    /// </summary>
    [Required]
    [MaxLength(260)] // Max file name length
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the image file in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the width of the image in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the image in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the SHA256 hash of the file content.
    /// </summary>
    [Required]
    [MaxLength(64)] // SHA256 hex string length
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full absolute path to the image file.
    /// </summary>
    [Required]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp of the file from the file system.
    /// </summary>
    public DateTime FileCreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp of the file from the file system.
    /// </summary>
    public DateTime FileLastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time the image was taken, extracted from EXIF data.
    /// </summary>
    public DateTime? DateTaken { get; set; }

    /// <summary>
    /// Gets or sets the camera model used to take the image, extracted from EXIF data.
    /// </summary>
    [MaxLength(255)]
    public string? CameraModel { get; set; }

    /// <summary>
    /// Gets or sets additional EXIF data stored as a JSON string.
    /// </summary>
    public string? ExifDataJson { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this record was created or last updated in the database.
    /// </summary>
    public DateTime ScannedAt { get; set; }
}
