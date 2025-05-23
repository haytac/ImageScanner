using ImageScanner.Cli.Commands;
using ImageScanner.Core.Interfaces;
using ImageScanner.Core.Interfaces.DataAccess;
using ImageScanner.Core.Models;
using ImageScanner.DataAccess;
using ImageScanner.DataAccess.Repositories;
using ImageScanner.Infrastructure.Logging;
using ImageScanner.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging; // Microsoft's ILogger
using Spectre.Console;
using Spectre.Console.Cli;

namespace ImageScanner.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Core.AppConstants.AppSettingsFileName, optional: true, reloadOnChange: true)
                .Build();

        }
        catch (Exception ex)
        {

            throw;
        }
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(builder =>
            {
                // Already added default appsettings.json by CreateDefaultBuilder if present
                // builder.AddConfiguration(configuration); // if needed to override
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.Configure<ImageScannerSettings>(hostContext.Configuration.GetSection("ImageScannerSettings"));

                // Register EF Core DbContext
                var dbSettings = hostContext.Configuration.GetSection("ImageScannerSettings").Get<ImageScannerSettings>();
                var dbPath = Path.IsPathRooted(dbSettings?.DatabasePath)
                             ? dbSettings.DatabasePath
                             : Path.Combine(Directory.GetCurrentDirectory(), dbSettings?.DatabasePath ?? Core.AppConstants.DefaultDbFileName);

                // Ensure directory for DB exists
                var dbDir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDir) && !System.IO.Directory.Exists(dbDir))
                {
                    System.IO.Directory.CreateDirectory(dbDir);
                }

                services.AddTransient<ScanImagesCommand>();
                services.AddTransient<ExitCommand>(); // Even if it has no dependencies now

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));

                // Register custom services
                services.AddSingleton<Core.Interfaces.ILoggerService, SpectreConsoleLogger>();
                services.AddSingleton<IFileSystemService, FileSystemService>();
                services.AddSingleton<IImageMetadataService, ImageMetadataService>();
                services.AddScoped<IImageRepository, ImageRepository>(); // Scoped for DbContext

                // For EF Core internal logging, if needed to be customized beyond appsettings.json
                services.AddLogging(loggingBuilder => {
                    loggingBuilder.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                    // Add console logger for Microsoft.Extensions.Logging if not already present by default
                    // loggingBuilder.AddConsole(); 
                });

            })
            .Build();

        // Apply migrations automatically on startup
        // This is good for development/small apps. For production, consider a separate migration step.
        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync(); // Ensure DB is created and migrations applied
                var logger = services.GetRequiredService<Core.Interfaces.ILoggerService>();
                logger.Info("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                var consoleLogger = services.GetRequiredService<Core.Interfaces.ILoggerService>();
                consoleLogger.Error(ex, "An error occurred while migrating the database.");
                AnsiConsole.WriteException(ex);
                return -1; // Indicate an error
            }
        }


        var app = new CommandApp(new TypeRegistrar(host.Services));
        app.Configure(config =>
        {
            config.SetApplicationName(Core.AppConstants.AppName);
            config.ValidateExamples();

            config.AddCommand<ScanImagesCommand>("scan")
                .WithDescription("Scans a folder for images, extracts metadata, and stores it in the database.")
                .WithExample(new[] { "scan", "--folder", "C:\\MyPictures" });

            config.AddCommand<ExitCommand>("exit")
               .WithDescription("Exits the application.");


#if DEBUG
            config.PropagateExceptions();
            config.ValidateExamples();
#endif

            config.SetExceptionHandler((ex, resolver) => {
                var logger = host.Services.GetRequiredService<Core.Interfaces.ILoggerService>();
                logger.Error("An unhandled application error occurred.");
                logger.RenderException(ex);
                return -1; // Error exit code
            });
        });

        // Interactive loop if no commands are passed
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine($"[bold deepskyblue1]Welcome to {Core.AppConstants.AppName}![/]");
            AnsiConsole.MarkupLine("Type '[yellow]help[/]' for a list of commands or '[yellow]<command> --help[/]' for command-specific help.");
            AnsiConsole.MarkupLine("Type '[yellow]exit[/]' to close the application.");
            while (true)
            {
                var input = AnsiConsole.Prompt(new TextPrompt<string>("[aqua]>[/]").AllowEmpty());
                if (string.IsNullOrWhiteSpace(input)) continue;

                var inputArgs = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (inputArgs.Length > 0 && inputArgs[0].Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine("[green]Exiting application...[/]");
                    break;
                }

                // If user types "help", convert to Spectre.Console.Cli standard "--help"
                if (inputArgs.Length > 0 && inputArgs[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    if (inputArgs.Length > 1) // e.g., "help scan"
                    {
                        inputArgs = new[] { inputArgs[1], "--help" };
                    }
                    else // just "help"
                    {
                        inputArgs = new[] { "--help" };
                    }
                }

                await app.RunAsync(inputArgs);
            }
            return 0;
        }

        return await app.RunAsync(args);
    }
}

/// <summary>
/// Type registrar for Spectre.Console.Cli using Microsoft.Extensions.DependencyInjection.
/// </summary>
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public TypeRegistrar(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public ITypeResolver Build() => new TypeResolver(_serviceProvider);

    /// <inheritdoc />
    public void Register(Type service, Type implementation)
    {
        // Handled by the main DI container setup.
        // Spectre.Console.Cli uses this to know about command types.
        // For commands, they are resolved from the _serviceProvider in TypeResolver.
    }

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation)
    {
        // Handled by the main DI container setup.
    }

    /// <inheritdoc />
    public void RegisterLazy(Type service, Func<object> func)
    {
        // Handled by the main DI container setup.
    }
}

/// <summary>
/// Type resolver for Spectre.Console.Cli.
/// </summary>
public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolver"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public TypeResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public object? Resolve(Type? type)
    {
        return type == null ? null : _serviceProvider.GetService(type);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // If _serviceProvider is a scope, dispose it here.
        // In this setup, it's the root provider, so DI host manages its lifetime.
    }
}