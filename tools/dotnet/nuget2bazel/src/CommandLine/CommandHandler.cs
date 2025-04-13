using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nuget2bazel.Core;
using nuget2bazel.Models;

namespace nuget2bazel.CommandLine;

/// <summary>
/// Handles the command line operations for the nuget2bazel tool.
/// </summary>
public class CommandHandler
{
    private readonly DirectoryInfo _workspaceDirectory;
    private readonly FileInfo _outputFile;
    private readonly string _packageSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHandler"/> class.
    /// </summary>
    /// <param name="workspaceDirectory">The workspace directory.</param>
    /// <param name="outputFile">The output file.</param>
    /// <param name="packageSource">The NuGet package source URL.</param>
    public CommandHandler(
        DirectoryInfo workspaceDirectory, 
        FileInfo outputFile, 
        string packageSource = "https://api.nuget.org/v3/index.json")
    {
        _workspaceDirectory = workspaceDirectory ?? throw new ArgumentNullException(nameof(workspaceDirectory));
        _outputFile = outputFile ?? throw new ArgumentNullException(nameof(outputFile));
        _packageSource = packageSource ?? throw new ArgumentNullException(nameof(packageSource));
    }

    /// <summary>
    /// Executes the command handler to process packages and generate the Bazel output file.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Searching for packages.lock.json files in {_workspaceDirectory.FullName}");
        
        var lockFiles = Directory.GetFiles(_workspaceDirectory.FullName, "packages.lock.json", SearchOption.AllDirectories);
        
        if (lockFiles.Length == 0)
        {
            Console.WriteLine("No packages.lock.json files found!");
            return;
        }
        
        Console.WriteLine($"Found {lockFiles.Length} packages.lock.json files");
        
        var allBazelPackages = new List<RulesDotnetNugetPackage>();
        var processedPackageIds = new HashSet<string>();
        
        foreach (var lockFile in lockFiles)
        {
            Console.WriteLine($"Processing {lockFile}");
            var packageAnalyzer = new NugetPackageAnalyzer(lockFile, _packageSource);
            var bazelNugetPackages = await packageAnalyzer.AnalyzePackagesAsync(cancellationToken);
            
            // Add unique packages
            foreach (var package in bazelNugetPackages.Where(package => !processedPackageIds.Contains(package.GetUniqueKey())))
            {
                allBazelPackages.Add(package);
                processedPackageIds.Add(package.GetUniqueKey());
            }
        }
        
        allBazelPackages = allBazelPackages.OrderBy(package => package.GetUniqueKey()).ToList();
        
        // Generate the json
        string content = BazelFileGenerator.Generate(allBazelPackages);
        
        // Write to file
        var outputDirectory = Path.GetDirectoryName(_outputFile.FullName)!;
        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllTextAsync(_outputFile.FullName, content, cancellationToken);
        
        Console.WriteLine($"Successfully wrote Bazel NuGet dependencies to {_outputFile.FullName}");
        Console.WriteLine($"Processed {allBazelPackages.Count} unique packages");
    }
}