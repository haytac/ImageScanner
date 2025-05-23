namespace ImageScanner.Core.Interfaces;


/// <summary>
/// Represents extracted image metadata including dimensions and EXIF data.
/// </summary>
public record ExtractedImageMetadata(
    int Width,
    int Height,
    Dictionary<string, string> ExifData);

/// <summary>
/// Interface for extracting metadata from image files.
/// </summary>
public interface IImageMetadataService
{
    /// <summary>
    /// Extracts dimensions and EXIF metadata from an image file.
    /// </summary>
    /// <param name="filePath">The path to the image file.</param>
    /// <param name="fieldsToExtract">Specific EXIF fields to prioritize.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the extracted metadata or null if extraction fails.</returns>
    Task<ExtractedImageMetadata?> ExtractMetadataAsync(string filePath, IEnumerable<string> fieldsToExtract, CancellationToken cancellationToken);
}
