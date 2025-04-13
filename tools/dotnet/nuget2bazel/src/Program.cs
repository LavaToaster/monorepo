using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using nuget2bazel.CommandLine;

// Create the root command for the application
var rootCommand = new RootCommand("NuGet package analyzer for Bazel");

// Add command line options
rootCommand.AddOption(CommandLineOptions.WorkspaceRootOption);
rootCommand.AddOption(CommandLineOptions.OutputFileOption);
rootCommand.AddOption(CommandLineOptions.PackageSourceOption);

// Set the command handler
rootCommand.SetHandler(async (workspaceRoot, outputFile, packageSource) =>
    {
        try
        {
            // If workspace root is not provided, try to get it from environment
            var workspaceDir = workspaceRoot ?? CommandLineOptions.GetWorkspaceDirectoryFromEnv();

            // If output file is not provided, default to nuget.deps.json in workspace root
            var outputFilePath = outputFile ??
                                 new FileInfo(Path.Combine(workspaceDir.FullName, "nuget.deps.json"));

            // Use default package source if not specified
            var nugetSource = packageSource ?? "https://api.nuget.org/v3/index.json";

            // Create and execute the command handler
            var handler = new CommandHandler(workspaceDir, outputFilePath, nugetSource);
            await handler.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine($"Inner Error: {ex.InnerException.Message}");
            }
        }
    },
    CommandLineOptions.WorkspaceRootOption,
    CommandLineOptions.OutputFileOption,
    CommandLineOptions.PackageSourceOption
);

// Execute the command with the provided arguments
await rootCommand.InvokeAsync(args);