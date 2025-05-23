using ImageScanner.Core.Models;
using ImageScanner.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace ImageScanner.DataAccess;


/// <summary>
/// EF Core DbContext for the Image Scanner application.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly string _databasePath;

    /// <summary>
    /// Gets or sets the DbSet for ImageInfo entities.
    /// </summary>
    public DbSet<ImageInfo> Images { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DbSet for ProcessedFile entities.
    /// </summary>
    public DbSet<ProcessedFile> ProcessedFiles { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// Used by EF Core tools.
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        // This constructor is used by DI and for migrations.
        // The path will be configured via options.
        // For design-time tools, it might need a parameterless constructor or a specific factory.
        // We will rely on the DI setup to provide the correct options.
        var settings = (IOptions<ImageScannerSettings>?)this.GetService(typeof(IOptions<ImageScannerSettings>));
        _databasePath = settings?.Value.DatabasePath ?? Core.AppConstants.DefaultDbFileName;
    }

    // Parameterless constructor for EF Core tools if needed, or use a design-time factory
    // public ApplicationDbContext()
    // {
    //    _databasePath = Core.AppConstants.DefaultDbFileName; // Fallback for tools
    // }


    /// <winheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_databasePath}");
        }
        base.OnConfiguring(optionsBuilder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ImageInfo>(entity =>
        {
            // Explicitly configure Id as ValueGeneratedOnAdd
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.Path).IsUnique();
            entity.HasIndex(e => e.FileHash);
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.ExifDataJson).HasColumnType("TEXT"); // Ensure JSON is stored as TEXT
        });

        modelBuilder.Entity<ProcessedFile>(entity =>
        {
            // Explicitly configure Id as ValueGeneratedOnAdd
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.Path).IsUnique();
            entity.HasIndex(e => e.FileHash);
        });
    }
}
