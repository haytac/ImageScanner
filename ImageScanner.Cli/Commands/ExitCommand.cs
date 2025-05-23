using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ImageScanner.Cli.Commands;

/// <summary>
/// Command to exit the application.
/// </summary>
[Description("Exits the application.")]
public sealed class ExitCommand : AsyncCommand
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context)
    {
        AnsiConsole.MarkupLine("[green]Exiting application...[/]");
        // Environment.Exit(0) could be used, but for graceful shutdown, 
        // it's better to let the command loop in Program.cs handle termination.
        // This command primarily serves the interactive mode.
        // For direct command line execution `app.exe exit`, it will run and then the app terminates.
        // In interactive mode, Program.cs loop will see "exit" and break.
        return Task.FromResult(0);
    }
}