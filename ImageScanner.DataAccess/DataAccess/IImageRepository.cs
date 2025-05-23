using ImageScanner.DataAccess.Entities; // Defined in DataAccess project

namespace ImageScanner.Core.Interfaces.DataAccess;

/// <summary>
/// Interface for repository operations related to images and processed files.
/// </summary>
public interface IImageRepository
{
    /// <summary>
    /// Adds or updates a batch of images to the database.
    /// </summary>
    /// <param name="images">The collection of images to add or update.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddOrUpdateImagesBatchAsync(IEnumerable<ImageInfo> images, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a processed file record by its path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The <see cref="ProcessedFile"/> if found; otherwise, null.</returns>
    Task<ProcessedFile?> GetProcessedFileByPathAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Adds or updates a processed file record.
    /// </summary>
    /// <param name="processedFile">The processed file record to add or update.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddOrUpdateProcessedFileAsync(ProcessedFile processedFile, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an image by its file path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The <see cref="ImageInfo"/> if found; otherwise, null.</returns>
    Task<ImageInfo?> GetImageByPathAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an image by its file hash.
    /// </summary>
    /// <param name="hash">The file hash.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The <see cref="ImageInfo"/> if found; otherwise, null.</returns>
    Task<ImageInfo?> GetImageByHashAsync(string hash, CancellationToken cancellationToken);
}