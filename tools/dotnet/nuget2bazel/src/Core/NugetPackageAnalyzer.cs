using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nuget2bazel.Models;
using nuget2bazel.Services;
using nuget2bazel.Utilities;
using NuGet.Packaging;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace nuget2bazel.Core;

/// <summary>
/// Analyzes NuGet packages from a lock file and converts them to Bazel package format.
/// </summary>
public class NugetPackageAnalyzer
{
    private readonly string _lockFilePath;
    private readonly PackageDownloadService _downloadService;
    private readonly PackageMetadataService _metadataService;
    private readonly PackagesLockFile _packagesLockFile;
    
    /// <summary>
    /// Gets the direct dependencies found in the package lock file.
    /// </summary>
    public List<DirectDependency> DirectDependencies { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NugetPackageAnalyzer"/> class.
    /// </summary>
    /// <param name="lockFilePath">The path to the packages.lock.json file.</param>
    /// <param name="packageSource">The NuGet package source URL.</param>
    public NugetPackageAnalyzer(string lockFilePath, string packageSource = "https://api.nuget.org/v3/index.json")
    {
        _lockFilePath = lockFilePath ?? throw new ArgumentNullException(nameof(lockFilePath));
        _downloadService = new PackageDownloadService(packageSource);
        _packagesLockFile = PackagesLockFileFormat.Read(lockFilePath);
        _metadataService = new PackageMetadataService(lockFilePath, FrameworkUtilities.ParsedFrameworks);
    }

    /// <summary>
    /// Analyzes the NuGet packages from the lock file and generates Bazel package representations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of Bazel NuGet packages.</returns>
    public async Task<List<RulesDotnetNugetPackage>> AnalyzePackagesAsync(CancellationToken cancellationToken = default)
    {
        var packages = new List<RulesDotnetNugetPackage>();
        var processedPackages = new HashSet<string>();

        // Process each package in the lock file
        foreach (var targetFramework in _packagesLockFile.Targets)
        {
            foreach (var library in targetFramework.Dependencies)
            {
                var packageKey = $"{library.Id}|{library.ResolvedVersion}";
                
                // Skip if we've already processed this package or if it's a project reference
                if (!processedPackages.Add(packageKey) || library.Type == PackageDependencyType.Project)
                    continue;
                
                // Track direct dependencies
                if (library.Type == PackageDependencyType.Direct)
                {
                    DirectDependencies.Add(new DirectDependency(
                        library.Id, 
                        library.ResolvedVersion.ToFullString()));
                }

                // Download and analyze the package
                var (hash, packageReader) = await _downloadService.DownloadPackageAsync(
                    library.Id, 
                    library.ResolvedVersion,
                    cancellationToken);

                // Create Bazel package representation
                var bazelPackage = CreateBazelPackage(library.Id, library.ResolvedVersion, hash, packageReader);
                packages.Add(bazelPackage);
            }
        }

        return packages;
    }

    private RulesDotnetNugetPackage CreateBazelPackage(
        string packageId, 
        NuGetVersion version, 
        string hash, 
        PackageReaderBase packageReader)
    {
        return new RulesDotnetNugetPackage
        {
            Id = packageId,
            Name = packageId,
            Version = version.ToFullString(),
            Sources = new[] { "https://api.nuget.org/v3/index.json" }, // TODO: Support private feeds
            Sha512 = hash,
            Dependencies = _metadataService.GetDependenciesPerTargetFramework(packageReader),
            TargetingPackOverrides = _metadataService.GetTargetingPackOverrides(packageReader),
            FrameworkList = _metadataService.GetFrameworkList(packageReader)
        };
    }
}