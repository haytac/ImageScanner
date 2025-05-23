using ImageScanner.Core.Interfaces.DataAccess;
using ImageScanner.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImageScanner.DataAccess.Repositories;


/// <summary>
/// Implements repository operations for image and processed file data.
/// </summary>
public class ImageRepository : IImageRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ImageRepository> _logger; // Standard ILogger for repository logs

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public ImageRepository(ApplicationDbContext context, ILogger<ImageRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddOrUpdateImagesBatchAsync(IEnumerable<ImageInfo> images, CancellationToken cancellationToken)
    {
        if (images == null || !images.Any())
        {
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var imageInBatch in images) // Renamed for clarity
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if an entity with this specific ID is already tracked or exists.
                // This handles the "moved file" scenario where imageInBatch.Id is already set.
                var trackedEntityWithSameId = _context.ChangeTracker.Entries<ImageInfo>()
                                                    .FirstOrDefault(e => e.Entity.Id == imageInBatch.Id && imageInBatch.Id != 0)?.Entity;

                ImageInfo? entityToProcess;

                if (trackedEntityWithSameId != null)
                {
                    entityToProcess = trackedEntityWithSameId; // Use the already tracked entity
                }
                else if (imageInBatch.Id != 0)
                {
                    // Entity has an ID, try to fetch it from DB to ensure we are updating it
                    entityToProcess = await _context.Images.FirstOrDefaultAsync(i => i.Id == imageInBatch.Id, cancellationToken);
                }
                else
                {
                    // imageInBatch.Id is 0, so it's definitely a new entity
                    entityToProcess = null;
                }


                if (entityToProcess != null) // This means imageInBatch.Id was non-zero and we found it (either tracked or in DB)
                {
                    // This is an UPDATE case.
                    // It could be an update to content for an existing path,
                    // OR it's the "moved file" (existingImageByHash) case where Path and other details are updated.
                    _logger.LogDebug($"Updating existing ImageInfo with Id: {entityToProcess.Id} for Path: {imageInBatch.Path}");

                    entityToProcess.Name = imageInBatch.Name;
                    entityToProcess.Path = imageInBatch.Path; // Critical for moved files
                    entityToProcess.Size = imageInBatch.Size;
                    entityToProcess.Width = imageInBatch.Width;
                    entityToProcess.Height = imageInBatch.Height;
                    entityToProcess.FileHash = imageInBatch.FileHash;
                    entityToProcess.FileCreatedAt = imageInBatch.FileCreatedAt;
                    entityToProcess.FileLastModifiedAt = imageInBatch.FileLastModifiedAt;
                    entityToProcess.DateTaken = imageInBatch.DateTaken;
                    entityToProcess.CameraModel = imageInBatch.CameraModel;
                    entityToProcess.ExifDataJson = imageInBatch.ExifDataJson;
                    entityToProcess.ScannedAt = DateTime.UtcNow;
                    // EF Core automatically tracks changes to 'entityToProcess' if it was fetched from _context.Images
                    // If it was from ChangeTracker, it's already tracked.
                    // If an entity is fetched, modified, SaveChangesAsync will generate an UPDATE.
                }
                else
                {
                    // This is a NEW entity (imageInBatch.Id was 0, or non-zero but not found in DB - though the latter is less likely with your command logic).
                    // The 'imageInBatch' itself is the new DTO-like object prepared by the command.
                    // Its Id should be 0.
                    _logger.LogDebug($"Adding new ImageInfo for Path: {imageInBatch.Path}. Initial Id from batch: {imageInBatch.Id}");
                    imageInBatch.ScannedAt = DateTime.UtcNow;
                    // Crucially, ensure Id is 0 if it's truly new, or EF Core must be configured to ignore pre-set IDs on Add.
                    // If ValueGeneratedOnAdd is working, imageInBatch.Id (being 0) is fine.
                    // If imageInBatch.Id was non-zero but not found, AddAsync might try to insert that ID.
                    // For safety, if it's truly "new" but somehow got an ID, you might reset it:
                    // if(imageInBatch.Id != 0) { /* log warning, potentially reset imageInBatch.Id = 0; if logic guarantees this is an error */ }

                    await _context.Images.AddAsync(imageInBatch, cancellationToken);
                }
            }
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Successfully added/updated batch of {Count} images.", images.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch image processing. Rolling back transaction.");
            await transaction.RollbackAsync(cancellationToken); // Make sure this RollbackAsync is also cancellable if appropriate
            throw;
        }
    }
    /// <inheritdoc />
    public async Task<ProcessedFile?> GetProcessedFileByPathAsync(string path, CancellationToken cancellationToken)
    {
        return await _context.ProcessedFiles
            .FirstOrDefaultAsync(pf => pf.Path == path, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddOrUpdateProcessedFileAsync(ProcessedFile processedFile, CancellationToken cancellationToken)
    {
        var existing = await _context.ProcessedFiles
            .FirstOrDefaultAsync(pf => pf.Path == processedFile.Path, cancellationToken);

        if (existing != null)
        {
            existing.FileHash = processedFile.FileHash;
            existing.LastProcessed = processedFile.LastProcessed;
            _context.ProcessedFiles.Update(existing);
        }
        else
        {
            await _context.ProcessedFiles.AddAsync(processedFile, cancellationToken);
        }
        // Note: SaveChangesAsync should be called by the calling service or as part of a larger transaction
        // For now, assuming it's called here for simplicity or if this is the unit of work.
        // In a batch scenario, SaveChangesAsync would be called after multiple AddOrUpdateProcessedFileAsync operations.
        // For this specific method, we'll save changes immediately.
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ImageInfo?> GetImageByPathAsync(string path, CancellationToken cancellationToken)
    {
        return await _context.Images
            .FirstOrDefaultAsync(i => i.Path == path, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ImageInfo?> GetImageByHashAsync(string hash, CancellationToken cancellationToken)
    {
        return await _context.Images
            .FirstOrDefaultAsync(i => i.FileHash == hash, cancellationToken);
    }
}
