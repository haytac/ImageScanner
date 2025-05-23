namespace ImageScanner.Core.Interfaces;


/// <summary>
/// Interface for file system operations.
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Recursively gets file paths from a directory matching specified extensions.
    /// </summary>
    /// <param name="folderPath">The path to the directory.</param>
    /// <param name="extensions">A collection of file extensions to include (e.g., ".jpg", ".png").</param>
    /// <param name="includeSubdirectories">Whether to search in subdirectories.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of file paths.</returns>
    IAsyncEnumerable<string> GetFilesAsync(string folderPath, IEnumerable<string> extensions, bool includeSubdirectories, CancellationToken cancellationToken);

    /// <summary>
    /// Calculates the SHA256 hash of a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The SHA256 hash as a hex string.</returns>
    Task<string> GetFileHashAsync(string filePath, CancellationToken cancellationToken);

    /// <summary>
    /// Gets basic file information.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>A <see cref="FileInfo"/> object.</returns>
    FileInfo GetFileInfo(string filePath);

    /// <summary>
    /// Checks if a directory exists.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>True if the directory exists, false otherwise.</returns>
    bool DirectoryExists(string path);
}
