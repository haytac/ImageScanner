using ImageScanner.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ImageScanner.Infrastructure.Services;


/// <summary>
/// Implements file system operations.
/// </summary>
public class FileSystemService : IFileSystemService
{
    private readonly ILoggerService _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemService"/> class.
    /// </summary>
    /// <param name="logger">The logger service.</param>
    public FileSystemService(ILoggerService logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetFilesAsync(string folderPath, IEnumerable<string> extensions, bool includeSubdirectories, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var normalizedExtensions = extensions.Select(ext => ext.StartsWith(".") ? ext : "." + ext).ToList();

        // Directory.EnumerateFiles can be slow for very large directories or network paths.
        // A custom recursive walk might offer more control for cancellation and error handling per file/directory.
        // For now, using the built-in method.

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(folderPath, "*.*", searchOption);
        }
        catch (DirectoryNotFoundException)
        {
            _logger.Error($"Directory not found: {folderPath}");
            yield break; // Exit if directory doesn't exist
        }
        catch (UnauthorizedAccessException)
        {
            _logger.Error($"Access denied to directory: {folderPath}");
            yield break;
        }


        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (normalizedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
            {
                // Yielding allows consumer to process files as they are found, improving responsiveness.
                await Task.Yield(); // Ensure async behavior and allow cancellation check
                yield return file;
            }
        }
    }

    /// <inheritdoc />
    public async Task<string> GetFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        // Using FileStream with a buffer for potentially large files.
        // FileShare.Read is important to allow other processes to read the file.
        const int bufferSize = 4096 * 2; // 8KB buffer

        try
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);

            var sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
        catch (FileNotFoundException)
        {
            _logger.Error($"File not found while hashing: {filePath}");
            return string.Empty;
        }
        catch (IOException ex) // Catches various I/O errors, e.g., file in use
        {
            _logger.Error(ex, $"IO error while hashing file: {filePath}");
            return string.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            _logger.Error($"Access denied while hashing file: {filePath}");
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public FileInfo GetFileInfo(string filePath)
    {
        return new FileInfo(filePath);
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }
}
