using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using ImageScanner.Core.Interfaces;
using ImageScanner.Core.Interfaces.DataAccess;
using ImageScanner.Core.Models;
using ImageScanner.DataAccess.Entities;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;
using ValidationResult = Spectre.Console.ValidationResult;

namespace ImageScanner.Cli.Commands;

/// <summary>
/// Command settings for scanning images.
/// </summary>
public sealed class ScanImagesSettings : CommandSettings
{
    [CommandOption("-f|--folder <FOLDER_PATH>")]
    [Description("The path to the folder to scan for images. Required.")]
    public string FolderPath { get; set; } = string.Empty;

    [CommandOption("-e|--extensions <EXTENSIONS>")]
    [Description("Comma-separated list of image extensions to scan (e.g., .png,.jpg). Overrides default settings.")]
    public string? Extensions { get; set; }

    [CommandOption("--min-size <MIN_SIZE_BYTES>")]
    [Description("Minimum file size in bytes. Files smaller than this will be skipped.")]
    [DefaultValue(null)]
    public long? MinFileSize { get; set; }

    [CommandOption("--max-size <MAX_SIZE_BYTES>")]
    [Description("Maximum file size in bytes. Files larger than this will be skipped.")]
    [DefaultValue(null)]
    public long? MaxFileSize { get; set; }

    [CommandOption("--no-subdirs")]
    [Description("Disable recursive scanning of subdirectories.")]
    [DefaultValue(false)]
    public bool NoSubdirectories { get; set; }

    [CommandOption("--batch-size <BATCH_SIZE>")]
    [Description("Number of records to process before committing to database. Overrides default settings.")]
    [DefaultValue(null)]
    public int? BatchSize { get; set; }

    [CommandOption("--summary")]
    [Description("Display a summary of the scan operation upon completion.")]
    [DefaultValue(false)]
    public bool ShowSummary { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            return ValidationResult.Error("Folder path is required.");
        }
        if (!System.IO.Directory.Exists(FolderPath))
        {
            return ValidationResult.Error($"The specified folder does not exist: {FolderPath}");
        }
        return ValidationResult.Success();
    }
}

/// <summary>
/// Command to scan images in a folder, extract metadata, and store it in the database.
/// </summary>
public sealed class ScanImagesCommand : AsyncCommand<ScanImagesSettings>
{
    private readonly ILoggerService _logger;
    private readonly IFileSystemService _fileSystemService;
    private readonly IImageMetadataService _imageMetadataService;
    private readonly IImageRepository _imageRepository;
    private readonly ImageScannerSettings _appSettings;

    // For cancellation
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public ScanImagesCommand(
        ILoggerService logger,
        IFileSystemService fileSystemService,
        IImageMetadataService imageMetadataService,
        IImageRepository imageRepository,
        IOptions<ImageScannerSettings> settingsOptions)
    {
        _logger = logger;
        _fileSystemService = fileSystemService;
        _imageMetadataService = imageMetadataService;
        _imageRepository = imageRepository;
        _appSettings = settingsOptions.Value;

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            _logger.Warning("Cancellation request received. Attempting to stop gracefully...");
            eventArgs.Cancel = true; // Prevent the process from terminating immediately
            _cancellationTokenSource.Cancel();
        };
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ScanImagesSettings settings)
    {
        _logger.Info($"Starting image scan for folder: {settings.FolderPath}");
        var cancellationToken = _cancellationTokenSource.Token;
        var stopwatch = Stopwatch.StartNew();

        var imageExtensions = settings.Extensions?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(e => e.Trim().ToLowerInvariant())
                              .ToList() ?? _appSettings.DefaultImageExtensions;
        var includeSubdirectories = !settings.NoSubdirectories;
        var batchSize = settings.BatchSize ?? _appSettings.DatabaseBatchSize;
        var minFileSize = settings.MinFileSize ?? _appSettings.MinFileSize;
        var maxFileSize = settings.MaxFileSize ?? _appSettings.MaxFileSize;
        if (maxFileSize == 0) maxFileSize = long.MaxValue; // Treat 0 as no upper limit

        _logger.Info($"Effective settings: Extensions: [{string.Join(", ", imageExtensions)}], Subdirectories: {includeSubdirectories}, BatchSize: {batchSize}, MinSize: {minFileSize}B, MaxSize: {(maxFileSize == long.MaxValue ? "Unlimited" : maxFileSize + "B")}");

        var filesToProcess = new List<string>();
        _logger.Info("Discovering image files...");
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Discovering files...", async ctx =>
            {
                await foreach (var file in _fileSystemService.GetFilesAsync(settings.FolderPath, imageExtensions, includeSubdirectories, cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    filesToProcess.Add(file);
                    ctx.Status($"Found {filesToProcess.Count} files...");
                }
            });

        if (cancellationToken.IsCancellationRequested) { _logger.Warning("File discovery cancelled."); return -1; }

        _logger.Info($"Found {filesToProcess.Count} potential image files to process.");
        if (filesToProcess.Count == 0)
        {
            _logger.Info("No image files found matching criteria. Scan complete.");
            stopwatch.Stop();
            _logger.Info($"Total time taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
            return 0;
        }

        var processedCount = 0;
        var skippedCount = 0;
        var errorCount = 0;
        var updatedCount = 0; // For files that were already in DB but hash changed
        var addedCount = 0;
        long totalBytesProcessed = 0;

        var imageBatch = new List<ImageInfo>();

        await AnsiConsole.Progress()
            .AutoClear(false) // Keep the progress bar after completion
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),    // Task description (what's happening)
                new ProgressBarColumn(),        // The progress bar itself
                new PercentageColumn(),         // Percentage of completion
                new RemainingTimeColumn(),      // Estimated time remaining
                new SpinnerColumn(Spinner.Known.Dots), // A spinner for visual feedback
            })
            .StartAsync(async ctx =>
            {
                var progressTask = ctx.AddTask("[green]Processing images[/]", new ProgressTaskSettings { MaxValue = filesToProcess.Count });

                foreach (var filePath in filesToProcess)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.Warning("Processing cancelled by user.");
                        break;
                    }

                    progressTask.Increment(1);
                    progressTask.Description = $"Processing: [yellow]{Path.GetFileName(filePath)}[/]";

                    try
                    {
                        var fileInfo = _fileSystemService.GetFileInfo(filePath);
                        if (fileInfo.Length < minFileSize || fileInfo.Length > maxFileSize)
                        {
                            _logger.Debug($"Skipping file {filePath} due to size constraints (Size: {fileInfo.Length}B).", Path.GetFileName(filePath));
                            skippedCount++;
                            continue;
                        }

                        var currentFileHash = await _fileSystemService.GetFileHashAsync(filePath, cancellationToken);
                        if (string.IsNullOrEmpty(currentFileHash))
                        {
                            _logger.Warning($"Could not compute hash for {filePath}. Skipping.", Path.GetFileName(filePath));
                            skippedCount++;
                            errorCount++;
                            continue;
                        }

                        var processedEntry = await _imageRepository.GetProcessedFileByPathAsync(filePath, cancellationToken);
                        if (processedEntry != null && processedEntry.FileHash == currentFileHash)
                        {
                            _logger.Debug($"File {filePath} is unchanged. Skipping.", Path.GetFileName(filePath));
                            skippedCount++;
                            totalBytesProcessed += fileInfo.Length; // Count as "processed" for summary
                            continue;
                        }

                        var metadata = await _imageMetadataService.ExtractMetadataAsync(filePath, _appSettings.MetadataFieldsToExtract, cancellationToken);
                        if (metadata == null)
                        {
                            _logger.Warning($"Could not extract metadata for {filePath}. Skipping.", Path.GetFileName(filePath));
                            skippedCount++;
                            errorCount++;
                            continue;
                        }

                        // Check for moved/renamed file (same hash, different path)
                        var existingImageByHash = await _imageRepository.GetImageByHashAsync(currentFileHash, cancellationToken);
                        ImageInfo imageToSave;

                        if (existingImageByHash != null && existingImageByHash.Path != filePath)
                        {
                            // File with same content exists at a different path. Assume it was moved/renamed.
                            _logger.Info($"File [yellow]{Path.GetFileName(filePath)}[/] with hash {currentFileHash.Substring(0, 7)}... seems to be a moved/renamed version of [yellow]{Path.GetFileName(existingImageByHash.Path)}[/]. Updating path.", Path.GetFileName(filePath));
                            existingImageByHash.Path = filePath; // Update path
                            existingImageByHash.Name = fileInfo.Name; // Update name
                            existingImageByHash.FileCreatedAt = fileInfo.CreationTimeUtc;
                            existingImageByHash.FileLastModifiedAt = fileInfo.LastWriteTimeUtc;
                            existingImageByHash.ScannedAt = DateTime.UtcNow;
                            // Other metadata might also need update if dimensions changed (unlikely for same hash)
                            // or if EXIF data is what we rely on for DateTaken etc.
                            imageToSave = existingImageByHash;
                            updatedCount++;
                        }
                        else
                        {
                            // New file or file content has changed
                            imageToSave = new ImageInfo
                            {
                                Name = fileInfo.Name,
                                Path = filePath,
                                Size = fileInfo.Length,
                                FileHash = currentFileHash,
                                Width = metadata.Width,
                                Height = metadata.Height,
                                FileCreatedAt = fileInfo.CreationTimeUtc,
                                FileLastModifiedAt = fileInfo.LastWriteTimeUtc,
                                ExifDataJson = metadata.ExifData.Any() ? JsonSerializer.Serialize(metadata.ExifData, new JsonSerializerOptions { WriteIndented = false }) : null,
                                ScannedAt = DateTime.UtcNow
                            };

                            if (metadata.ExifData.TryGetValue("Date/Time Original", out var dateTakenStr))
                            {
                                if (DateTime.TryParseExact(dateTakenStr, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTaken))
                                    imageToSave.DateTaken = dateTaken;
                            }
                            if (metadata.ExifData.TryGetValue("Model", out var cameraModel))
                            {
                                imageToSave.CameraModel = cameraModel;
                            }
                            addedCount++;
                        }

                        imageBatch.Add(imageToSave);
                        _logger.Log(LogLevel.Info, $"Prepared for DB: {imageToSave.Name} (Size: {imageToSave.Size}B, Dim: {imageToSave.Width}x{imageToSave.Height})", Path.GetFileName(filePath));

                        if (imageBatch.Count >= batchSize || (processedCount + skippedCount + errorCount == filesToProcess.Count - 1 && imageBatch.Any())) // last file and batch has items
                        {
                            _logger.Debug($"Writing batch of {imageBatch.Count} images to database...");
                            progressTask.Description = "[aqua]Saving batch to DB...[/]";
                            await _imageRepository.AddOrUpdateImagesBatchAsync(imageBatch, cancellationToken);

                            foreach (var img in imageBatch)
                            {
                                await _imageRepository.AddOrUpdateProcessedFileAsync(new ProcessedFile
                                {
                                    Path = img.Path,
                                    FileHash = img.FileHash,
                                    LastProcessed = DateTime.UtcNow
                                }, cancellationToken);
                                totalBytesProcessed += img.Size;
                            }
                            imageBatch.Clear();
                            _logger.Success($"Batch committed. Processed so far: {processedCount + 1}");
                        }
                        processedCount++;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Warning("Operation cancelled during file processing loop.");
                        break; // Exit loop
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Failed to process file: {filePath}. Skipping.");
                        errorCount++;
                    }
                }
                // Process any remaining items in the batch
                if (imageBatch.Any() && !cancellationToken.IsCancellationRequested)
                {
                    _logger.Debug($"Writing final batch of {imageBatch.Count} images to database...");
                    progressTask.Description = "[aqua]Saving final batch to DB...[/]";
                    await _imageRepository.AddOrUpdateImagesBatchAsync(imageBatch, cancellationToken);
                    foreach (var img in imageBatch)
                    {
                        await _imageRepository.AddOrUpdateProcessedFileAsync(new ProcessedFile
                        {
                            Path = img.Path,
                            FileHash = img.FileHash,
                            LastProcessed = DateTime.UtcNow
                        }, cancellationToken);
                        totalBytesProcessed += img.Size;
                    }
                    imageBatch.Clear();
                    _logger.Success("Final batch committed.");
                }
                progressTask.StopTask();
            });

        stopwatch.Stop();
        _logger.Success("Scan operation finished.");

        if (settings.ShowSummary || cancellationToken.IsCancellationRequested) // Also show summary if cancelled to see progress
        {
            var summaryTable = new Table()
                .Title("Scan Summary")
                .Border(TableBorder.Rounded)
                .AddColumn("Metric")
                .AddColumn("Value");

            summaryTable.AddRow("Total Files Found", filesToProcess.Count.ToString());
            summaryTable.AddRow("[green]Files Processed (Added/Updated)[/]", processedCount.ToString());
            summaryTable.AddRow("  [lightgreen]New Files Added[/]", addedCount.ToString());
            summaryTable.AddRow("  [lightgreen]Existing Files Updated (Moved/Content Changed)[/]", updatedCount.ToString());
            summaryTable.AddRow("[yellow]Files Skipped (Unchanged/Size Filter)[/]", skippedCount.ToString());
            summaryTable.AddRow("[red]Files with Errors[/]", errorCount.ToString());
            summaryTable.AddRow("Total Bytes Processed", $"{totalBytesProcessed / (1024.0 * 1024.0):F2} MB");
            summaryTable.AddRow("Total Time Taken", $"{stopwatch.Elapsed.TotalSeconds:F2} seconds");
            if (cancellationToken.IsCancellationRequested)
            {
                summaryTable.AddRow("[bold red]Status[/]", "[bold red]Cancelled by user[/]");
            }
            else
            {
                summaryTable.AddRow("[bold green]Status[/]", "[bold green]Completed[/]");
            }

            AnsiConsole.Write(summaryTable);
        }

        return cancellationToken.IsCancellationRequested ? -1 : 0; // Indicate error if cancelled
    }
}