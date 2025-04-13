using System;
using System.CommandLine;
using System.IO;

namespace nuget2bazel.CommandLine;

/// <summary>
/// Defines the command line options for the nuget2bazel tool.
/// </summary>
public static class CommandLineOptions
{
    /// <summary>
    /// Gets the workspace root directory option.
    /// </summary>
    public static Option<DirectoryInfo?> WorkspaceRootOption { get; } = new(
        name: "--workspace-root",
        description: "Path to the workspace root directory (defaults to BUILD_WORKSPACE_DIRECTORY env var if not specified)"
    )
    {
        IsRequired = false
    };

    /// <summary>
    /// Gets the output file option.
    /// </summary>
    public static Option<FileInfo?> OutputFileOption { get; } = new(
        name: "--output-file",
        description: "Path to output file (defaults to nuget_deps.bzl in workspace root if not specified)"
    )
    {
        IsRequired = false
    };
    
    /// <summary>
    /// Gets the package source option.
    /// </summary>
    public static Option<string> PackageSourceOption { get; } = new(
        name: "--package-source",
        description: "NuGet package source URL (defaults to nuget.org if not specified)"
    )
    {
        IsRequired = false
    };

    /// <summary>
    /// Gets the workspace directory from the environment variable if available.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the workspace root directory is not provided and BUILD_WORKSPACE_DIRECTORY environment variable is not set.
    /// </exception>
    public static DirectoryInfo GetWorkspaceDirectoryFromEnv()
    {
        var workspaceDir = Environment.GetEnvironmentVariable("BUILD_WORKSPACE_DIRECTORY");
        
        if (string.IsNullOrEmpty(workspaceDir))
        {
            throw new InvalidOperationException(
                "Workspace root directory not provided and BUILD_WORKSPACE_DIRECTORY environment variable is not set. " +
                "Please either specify --workspace-root or run this tool in a Bazel environment."
            );
        }
        
        return new DirectoryInfo(workspaceDir);
    }
}