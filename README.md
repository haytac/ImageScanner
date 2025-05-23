# ImageScanner CLI ✨

ImageScanner CLI is a powerful and extensible .NET console application designed to scan directories for image files, extract metadata (dimensions, EXIF data, etc.), and store this information efficiently in an SQLite database. It leverages modern .NET features, asynchronous programming, and Entity Framework Core for robust performance and data management.

[![.NET Version](https://img.shields.io/badge/.NET-8.0-blueviolet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
<!-- Add other badges as applicable, e.g., build status, NuGet version -->

## Table of Contents

- [Features](#features-)
- [Prerequisites](#prerequisites-)
- [Getting Started](#getting-started-)
  - [Cloning the Repository](#cloning-the-repository)
  - [Restoring Dependencies](#restoring-dependencies)
- [Configuration](#configuration-%EF%B8%8F)
- [Usage](#usage-)
  - [Running the Application](#running-the-application)
  - [Interactive Mode](#interactive-mode)
  - [Available Commands](#available-commands)
    - [`scan`](#scan-command)
    - [`exit`](#exit-command)
    - [`help`](#help-command)
- [Project Structure](#project-structure-)
- [Database](#database-)
  - [Schema](#schema)
  - [Migrations](#migrations)
- [Development](#development-%EF%B8%8F)
  - [Building the Project](#building-the-project)
  - [Working with EF Core Migrations](#working-with-ef-core-migrations)
- [Contributing](#contributing-)
- [License](#license-)

## Features 🚀

*   **Recursive Folder Scanning**: Scans specified folders for image files, with an option to include subdirectories.
*   **Configurable File Types**: Supports a configurable list of image extensions (e.g., `.jpg`, `.png`, `.gif`, `.bmp`).
*   **File Size Filtering**: Allows specifying minimum and maximum file sizes to process.
*   **Advanced Metadata Extraction**: Extracts image dimensions (width, height) and detailed EXIF data (camera model, date taken, exposure, etc.) using the `MetadataExtractor` library.
*   **SQLite Database Storage**: Persists image information and metadata in a local SQLite database via Entity Framework Core.
*   **Asynchronous Operations**: All I/O-bound operations (file system access, database queries, metadata extraction) are fully asynchronous for improved performance and responsiveness.
*   **Cancellation Support**: Long-running operations like scanning can be gracefully cancelled using `Ctrl+C`.
*   **Batch Processing**: Images are processed and saved to the database in configurable batches for efficiency.
*   **Duplicate/Change Detection**:
    *   Avoids re-processing unchanged files based on path and file hash.
    *   Updates records if file content (hash) changes.
    *   Handles moved/renamed files by updating the path if the hash matches an existing record.
*   **Rich Console Output**: Utilizes `Spectre.Console` for user-friendly, colorful logging, progress bars, and summary tables.
*   **Interactive Mode**: Provides an interactive prompt for executing commands if no arguments are supplied at startup.
*   **Configuration via `appsettings.json`**: Key application settings are managed through a JSON configuration file.
*   **SOLID Principles & Modularity**: Designed with SOLID principles and a clear, modular project structure for maintainability and extensibility.

## Prerequisites 🛠️

*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or the latest .NET SDK version targeted by the project)

## Getting Started 🏁

### Cloning the Repository

```bash
git clone https://your-repository-url/ImageScanner.git
cd ImageScanner
```

### Restoring Dependencies

Restore the necessary NuGet packages for all projects in the solution:

```bash
dotnet restore ImageScanner.sln
```

The application is configured to automatically apply Entity Framework Core migrations on startup, so the database schema will be created or updated when you first run the application.

## Configuration ⚙️

Application behavior can be configured via the `appsettings.json` file located in the output directory of the `ImageScanner.Cli` project. If this file is not present, sensible defaults will be used.

Key configuration options under the `ImageScannerSettings` section:

*   `DefaultImageExtensions`: Array of image file extensions to scan (e.g., `[ ".png", ".jpg" ]`).
*   `DatabaseBatchSize`: Number of image records to process before committing to the database.
*   `DatabasePath`: Path to the SQLite database file. Can be absolute or relative to the execution directory.
*   `LogFilePath`: Path for a persistent log file (Note: current implementation focuses on console logging).
*   `MetadataFieldsToExtract`: Specific EXIF metadata fields to extract. Use `"*"` for all available fields recognized by `MetadataExtractor`.
*   `MinFileSize`: Minimum file size in bytes for an image to be processed (0 for no minimum).
*   `MaxFileSize`: Maximum file size in bytes (0 for no maximum).

Example `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ImageScannerSettings": {
    "DefaultImageExtensions": [ ".png", ".jpg", ".jpeg", ".bmp", ".gif" ],
    "DatabaseBatchSize": 50,
    "DatabasePath": "Data/ImageScannerDB.sqlite",
    "LogFilePath": "Logs/ImageScannerApp.log",
    "MetadataFieldsToExtract": [
      "Make",
      "Model",
      "Date/Time Original",
      "Exposure Time",
      "F-Number"
    ],
    "MinFileSize": 1024, // 1KB
    "MaxFileSize": 0     // No limit
  }
}
```

## Usage 💡

### Running the Application

After building the project (see [Building the Project](#building-the-project)), you can run the CLI tool from the output directory (e.g., `ImageScanner.Cli/bin/Debug/net8.0/`).

```bash
# On Windows
ImageScanner.Cli.exe <command> [options]

# On Linux/macOS
./ImageScanner.Cli <command> [options]
```

### Interactive Mode

If you run the application without any arguments, it will start in interactive mode:

```bash
ImageScanner.Cli.exe
```

You'll be greeted with a welcome message and a prompt (`>`). Type commands directly into the prompt.

```
Welcome to ImageScanner!
Type 'help' for a list of commands or '<command> --help' for command-specific help.
Type 'exit' to close the application.
> scan --folder "C:\Users\YourUser\Pictures"
```

### Available Commands

#### `scan` Command

Scans a folder for images, extracts metadata, and stores it in the database.

**Syntax:**

```bash
scan -f|--folder <FOLDER_PATH> [options]
```

**Required Argument:**

*   `-f|--folder <FOLDER_PATH>`: The path to the folder to scan for images.

**Options:**

*   `-e|--extensions <EXTENSIONS>`: Comma-separated list of image extensions (e.g., `.png,.jpg`). Overrides defaults from `appsettings.json`.
*   `--min-size <MIN_SIZE_BYTES>`: Minimum file size in bytes.
*   `--max-size <MAX_SIZE_BYTES>`: Maximum file size in bytes.
*   `--no-subdirs`: Disable recursive scanning of subdirectories.
*   `--batch-size <BATCH_SIZE>`: Database commit batch size. Overrides `appsettings.json`.
*   `--summary`: Display a scan summary upon completion.

**Examples:**

1.  Scan a folder with default settings:
    ```bash
    scan --folder "D:\MyPhotos"
    ```
2.  Scan a folder for only JPG and PNG files, including subdirectories, and show a summary:
    ```bash
    scan --folder "/mnt/data/images" --extensions ".jpg,.png" --summary
    ```
3.  Scan a folder, excluding subdirectories, and set a minimum file size of 10KB:
    ```bash
    scan --folder "C:\Shared\Pics" --no-subdirs --min-size 10240
    ```

#### `exit` Command

Exits the application (primarily for interactive mode).

**Syntax:**

```bash
exit
```

#### `help` Command

Displays help information.

*   Show general help and list of commands:
    ```bash
    help
    ```
    (In interactive mode) or
    ```bash
    ImageScanner.Cli.exe --help
    ```
    (When run directly)

*   Show help for a specific command (e.g., `scan`):
    ```bash
    help scan
    ```
    (In interactive mode) or
    ```bash
    ImageScanner.Cli.exe scan --help
    ```
    (When run directly)

## Project Structure 📂

The solution is organized into four main projects:

*   **`ImageScanner.Cli`**: The main executable console application. Contains command definitions, `Program.cs` for setup, and handles user interaction.
*   **`ImageScanner.Core`**: Contains core abstractions (interfaces like `ILoggerService`, `IFileSystemService`, `IImageRepository`), domain models/DTOs (`ImageScannerSettings`), and constants. It has minimal dependencies.
*   **`ImageScanner.DataAccess`**: Manages all database interactions using Entity Framework Core. Contains the `DbContext`, entity classes (`ImageInfo`, `ProcessedFile`), and repository implementations.
*   **`ImageScanner.Infrastructure`**: Provides concrete implementations for interfaces defined in `ImageScanner.Core` (e.g., `SpectreConsoleLogger`, `FileSystemService`, `ImageMetadataService`).

This separation promotes loose coupling, testability, and maintainability.

## Database 💾

### Schema

The application uses an SQLite database with two main tables:

1.  **`Images`**: Stores detailed information about each scanned image.
    *   `Id` (Primary Key, Auto-increment)
    *   `Name` (File name)
    *   `Path` (Full file path, unique index)
    *   `Size` (File size in bytes)
    *   `Width`, `Height` (Image dimensions)
    *   `FileHash` (SHA256 hash of file content, indexed)
    *   `FileCreatedAt`, `FileLastModifiedAt` (File system timestamps)
    *   `DateTaken` (From EXIF)
    *   `CameraModel` (From EXIF)
    *   `ExifDataJson` (Other extracted EXIF data as JSON)
    *   `ScannedAt` (Timestamp of when the record was last scanned/updated)
2.  **`ProcessedFiles`**: Tracks files that have been processed to optimize rescans.
    *   `Id` (Primary Key, Auto-increment)
    *   `Path` (Full file path, unique index)
    *   `FileHash` (SHA256 hash of file content)
    *   `LastProcessed` (Timestamp of last processing)

### Migrations

Entity Framework Core migrations are used to manage the database schema. The application is configured to automatically apply pending migrations on startup (`context.Database.MigrateAsync()` in `Program.cs`). This ensures the database schema is up-to-date when the application runs.

## Development 🧑‍💻

### Building the Project

To build the solution:

```bash
dotnet build ImageScanner.sln --configuration Release
```

The executable will be in the respective `bin/Release/net8.0` folder of the `ImageScanner.Cli` project.

### Working with EF Core Migrations

If you make changes to the entity models in `ImageScanner.DataAccess.Entities` or `DbContext` configurations, you'll need to add a new migration and potentially update the database.

**Prerequisites for EF Core tools:**

Ensure you have the EF Core tools installed globally or locally. For global installation:
`dotnet tool install --global dotnet-ef`

**Adding a Migration:**

Navigate to the solution root or a project directory in your terminal.

```bash
dotnet ef migrations add <MigrationName> --project ImageScanner.DataAccess --startup-project ImageScanner.Cli
```

Replace `<MigrationName>` with a descriptive name for your migration (e.g., `AddImageDescriptionField`).

**Updating the Database (Applying Migrations Manually):**

While the app applies migrations automatically, you can also do it manually:

```bash
dotnet ef database update --project ImageScanner.DataAccess --startup-project ImageScanner.Cli
```

**Removing the Last Migration (if something went wrong):**

```bash
dotnet ef migrations remove --project ImageScanner.DataAccess --startup-project ImageScanner.Cli
```

## Contributing 🤝

Contributions are welcome! If you'd like to contribute, please follow these general steps:

1.  Fork the repository.
2.  Create a new branch for your feature or bug fix (`git checkout -b feature/your-feature-name`).
3.  Make your changes and commit them with clear, descriptive messages.
4.  Ensure your code adheres to the project's coding style and all tests pass.
5.  Push your changes to your forked repository.
6.  Create a Pull Request to the main repository's `main` branch.

Please ensure your PR includes a clear description of the changes and why they are needed.