using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using nuget2bazel.Utilities;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace nuget2bazel.Services;

/// <summary>
/// Provides functionality for downloading NuGet packages.
/// </summary>
public class PackageDownloadService
{
    private const string DefaultPackageSource = "https://api.nuget.org/v3/index.json";
    
    private readonly string _packageSource;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PackageDownloadService"/> class.
    /// </summary>
    /// <param name="packageSource">The NuGet package source URL.</param>
    public PackageDownloadService(string packageSource = DefaultPackageSource)
    {
        _packageSource = packageSource ?? throw new ArgumentNullException(nameof(packageSource));
    }

    /// <summary>
    /// Downloads a NuGet package and calculates its hash.
    /// </summary>
    /// <param name="packageId">The package identifier.</param>
    /// <param name="version">The package version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the package hash and the package reader.</returns>
    public async Task<(string Hash, PackageReaderBase PackageReader)> DownloadPackageAsync(
        string packageId, 
        NuGetVersion version,
        CancellationToken cancellationToken = default)
    {
        var packageIdentity = new PackageIdentity(packageId, version);
        var packageSource = new PackageSource(_packageSource);
        var sourceRepository = new SourceRepository(packageSource, Repository.Provider.GetCoreV3());
        var downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>(cancellationToken);
        var cacheContext = new SourceCacheContext();
        var downloadContext = new PackageDownloadContext(cacheContext);

        using var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
            packageIdentity,
            downloadContext,
            Path.GetTempPath(),
            NullLogger.Instance,
            cancellationToken);

        if (downloadResult.Status != DownloadResourceResultStatus.Available)
            throw new InvalidOperationException($"Failed to download package {packageId} version {version}");

        var archiveHash = HashUtilities.CalculateSha512Hash(downloadResult.PackageStream);

        return (archiveHash, downloadResult.PackageReader);
    }
}