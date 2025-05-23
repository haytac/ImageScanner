using ImageScanner.Core.Interfaces;
using ImageScanner.Core.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Bmp;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Gif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Png;
using Microsoft.Extensions.Options;

namespace ImageScanner.Infrastructure.Services;


/// <summary>
/// Implements metadata extraction from image files using MetadataExtractor.
/// </summary>
public class ImageMetadataService : IImageMetadataService
{
    private readonly ILoggerService _logger;
    private readonly ImageScannerSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageMetadataService"/> class.
    /// </summary>
    /// <param name="logger">The logger service.</param>
    /// <param name="settingsOptions">Application settings.</param>
    public ImageMetadataService(ILoggerService logger, IOptions<ImageScannerSettings> settingsOptions)
    {
        _logger = logger;
        _settings = settingsOptions.Value;
    }

    /// <inheritdoc />
    public async Task<ExtractedImageMetadata?> ExtractMetadataAsync(string filePath, IEnumerable<string> fieldsToExtract, CancellationToken cancellationToken)
    {
        try
        {
            // MetadataExtractor reads the file synchronously internally in ReadMetadata method.
            // To make this truly async if reading large headers becomes a bottleneck,
            // one would need to read parts of the file into a MemoryStream async first,
            // but MetadataExtractor doesn't directly support async stream reading.
            // For typical image metadata, this synchronous portion is usually fast.
            // We'll wrap it in Task.Run to ensure it doesn't block the calling async thread for long.
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                IEnumerable<MetadataExtractor.Directory> directories;
                try
                {
                    // Using FileStream to ensure proper disposal and error handling for file access
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    directories = ImageMetadataReader.ReadMetadata(stream);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    _logger.Warning($"Cannot access or read file for metadata: {filePath}. Error: {ex.Message}");
                    return null;
                }
                catch (ImageProcessingException ex) // MetadataExtractor specific exception
                {
                    _logger.Warning($"Error processing image metadata for {filePath}: {ex.Message}");
                    return null; // Potentially corrupt or unsupported image format for metadata
                }


                int width = 0;
                int height = 0;

                // Try to get dimensions from various directory types
                var jpegDir = directories.OfType<JpegDirectory>().FirstOrDefault();
                if (jpegDir != null)
                {
                    width = jpegDir.GetImageWidth();
                    height = jpegDir.GetImageHeight();
                }

                if (width == 0 || height == 0)
                {
                    var pngDir = directories.OfType<PngDirectory>().FirstOrDefault();
                    if (pngDir != null)
                    {
                        if (pngDir.TryGetInt32(PngDirectory.TagImageWidth, out var w)) width = w;
                        if (pngDir.TryGetInt32(PngDirectory.TagImageHeight, out var h)) height = h;
                    }
                }
                if (width == 0 || height == 0)
                {
                    var gifHeaderDir = directories.OfType<GifHeaderDirectory>().FirstOrDefault();
                    if (gifHeaderDir != null)
                    {
                        if (gifHeaderDir.TryGetInt32(GifHeaderDirectory.TagImageWidth, out var w)) width = w;
                        if (gifHeaderDir.TryGetInt32(GifHeaderDirectory.TagImageHeight, out var h)) height = h;
                    }
                }
                if (width == 0 || height == 0)
                {
                    var bmpHeaderDir = directories.OfType<BmpHeaderDirectory>().FirstOrDefault();
                    if (bmpHeaderDir != null)
                    {
                        if (bmpHeaderDir.TryGetInt32(BmpHeaderDirectory.TagImageWidth, out var w)) width = w;
                        if (bmpHeaderDir.TryGetInt32(BmpHeaderDirectory.TagImageHeight, out var h)) height = h;
                    }
                }
                if (width == 0 || height == 0) // Fallback to Exif IFD0 if available
                {
                    var ifd0Dir = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                    if (ifd0Dir != null)
                    {
                        if (ifd0Dir.TryGetInt32(ExifDirectoryBase.TagImageWidth, out var w)) width = w;
                        if (ifd0Dir.TryGetInt32(ExifDirectoryBase.TagImageHeight, out var h)) height = h;
                    }
                }
                if (width == 0 || height == 0) // Fallback to Exif SubIFD if available
                {
                    var subIfdDir = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                    if (subIfdDir != null)
                    {
                        if (subIfdDir.TryGetInt32(ExifDirectoryBase.TagExifImageWidth, out var w)) width = w;
                        if (subIfdDir.TryGetInt32(ExifDirectoryBase.TagExifImageHeight, out var h)) height = h;
                    }
                }


                var exifData = new Dictionary<string, string>();
                var requestedFields = new HashSet<string>(fieldsToExtract ?? _settings.MetadataFieldsToExtract, StringComparer.OrdinalIgnoreCase);

                foreach (var directory in directories)
                {
                    foreach (var tag in directory.Tags)
                    {
                        if (requestedFields.Contains(tag.Name) || requestedFields.Contains("*")) // Allow wildcard for all
                        {
                            if (tag.Description != null)
                            {
                                exifData[tag.Name] = tag.Description;
                            }
                        }
                    }
                }
                return new ExtractedImageMetadata(width, height, exifData);

            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning($"Metadata extraction cancelled for {filePath}.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Unexpected error extracting metadata for {filePath}.");
            return null;
        }
    }
}
