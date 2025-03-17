using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using nuget_query.Models;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace nuget_query;

public class PackageAnalyzer(string projectLockFilePath)
{
    PackagesLockFile packagesLockFile = PackagesLockFileFormat.Read(projectLockFilePath);
    public List<string[]> DirectDependencies = [];
    
    
    public async Task<List<BazelNugetPackage>> GetBazelNugetPackages()
    {
        var list = new List<BazelNugetPackage>();
        var seen = new HashSet<string>();

        foreach (var targetFramework in packagesLockFile.Targets)
        foreach (var library in targetFramework.Dependencies)
        {
            var key = $"{library.Id}{library.ResolvedVersion}";
            
            if (!seen.Add(key)) continue;
            if (library.Type == PackageDependencyType.Project) continue;
            if (library.Type == PackageDependencyType.Direct)
            {
                DirectDependencies.Add([library.Id, library.ResolvedVersion.ToFullString()]);
            }

            var (archiveHash, packageReader) = await DownloadPackage(library.Id, library.ResolvedVersion);

            list.Add(new BazelNugetPackage
            {
                Id = library.Id,
                Name = library.Id,
                Version = library.ResolvedVersion.ToFullString(),
                Sources = ["https://api.nuget.org/v3/index.json"], // TODO: Private feeds?
                Sha512 = archiveHash,
                Dependencies = GetDependenciesPerTfm(packageReader),
                TargetingPackOverrides = GetOverrides(packageReader),
                FrameworkList = GetFrameworkList(packageReader),
            });
            seen.Add(key);
        }

        return list;
    }

    private static string[] GetOverrides(PackageReaderBase packageReader)
    {
        var overridePath = packageReader.GetItems("data")
            .SelectMany(f => f.Items)
            .FirstOrDefault(f => f.EndsWith("PackageOverrides.txt"));

        if (overridePath == null) return [];
        
        var file = packageReader.GetStream(overridePath);
        using var reader = new StreamReader(file);
        return reader.ReadToEnd().Split("\r\n");

    }
    
    private static string[] GetFrameworkList(PackageReaderBase packageReader)
    {
        var frameworkListPath = packageReader.GetItems("data")
            .SelectMany(f => f.Items)
            .FirstOrDefault(f => f.EndsWith("FrameworkList.xml"));

        if (frameworkListPath == null) return [];
        
        var file = packageReader.GetStream(frameworkListPath);
        var xmlDocument = new XmlDocument();
        xmlDocument.Load(file);
            
        var root = xmlDocument.DocumentElement;
        var managedAssemblies = new List<string>();
            
        foreach (XmlNode node in root!.ChildNodes)
        {
            if (node.Attributes?["Type"]?.Value != "Managed") continue;

            var name = node.Attributes["AssemblyName"]?.Value;
            var version = node.Attributes["AssemblyVersion"]?.Value;
                    
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(version))
            {
                managedAssemblies.Add($"{name}|{version}");
            }
        }
            
        return managedAssemblies.ToArray();
    }

    private Dictionary<string, List<string>> GetDependenciesPerTfm(PackageReaderBase package)
    {
        var frameworkReducer = new FrameworkReducer();
        var packageDependencies = package.GetPackageDependencies();
        var bazelDependencies = new Dictionary<string, List<string>>();

        foreach (var tfm in _frameworks)
        {
            var nearest = frameworkReducer.GetNearest(tfm, packageDependencies.Select(x => x.TargetFramework));
            var frameworkDependencies = packageDependencies.Where(x => x.TargetFramework.Equals(nearest)).ToList();

            var sfn = tfm.GetShortFolderName();

            bazelDependencies[sfn] = [];

            foreach (var pkg in frameworkDependencies.SelectMany(fwd => fwd.Packages))
            {
                // check if pkgid is in lockfile
                var exists = packagesLockFile.Targets.SelectMany(t => t.Dependencies)
                    .Any(d => d.Id == pkg.Id);

                if (exists) bazelDependencies[sfn].Add(pkg.Id);
            }
        }

        return bazelDependencies;
    }

    private async Task<(string archiveHash, PackageReaderBase PackageReader)> DownloadPackage(string id,
        NuGetVersion version)
    {
        var packageIdentity = new PackageIdentity(id, version);
        var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
        var sourceRepository = new SourceRepository(packageSource, Repository.Provider.GetCoreV3());
        var downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>();
        var cacheContext = new SourceCacheContext();
        var downloadContext = new PackageDownloadContext(cacheContext);

        using var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
            packageIdentity,
            downloadContext,
            Path.GetTempPath(),
            NullLogger.Instance,
            CancellationToken.None);

        if (downloadResult.Status != DownloadResourceResultStatus.Available)
            throw new Exception($"Failed to download package {id} version {version}");

        var archiveHash = CalculateSha512Hash(downloadResult.PackageStream);

        return (archiveHash, downloadResult.PackageReader);
    }

    private static string CalculateSha512Hash(Stream stream, bool resetPosition = true)
    {
        var originalPosition = stream.Position;

        try
        {
            using var sha512 = SHA512.Create();
            var base64 = Convert.ToBase64String(sha512.ComputeHash(stream));
            
            return $"sha512-{base64}";
        }
        finally
        {
            // Reset the stream position if requested
            if (resetPosition)
            {
                stream.Position = originalPosition;
            }
        }
    }
    
    private static readonly List<string> tfms =
    [
        "netstandard",
        "netstandard1.0",
        "netstandard1.1",
        "netstandard1.2",
        "netstandard1.3",
        "netstandard1.4",
        "netstandard1.5",
        "netstandard1.6",
        "netstandard2.0",
        "netstandard2.1",
        "net11",
        "net20",
        "net30",
        "net35",
        "net40",
        "net403",
        "net45",
        "net451",
        "net452",
        "net46",
        "net461",
        "net462",
        "net47",
        "net471",
        "net472",
        "net48",
        "netcoreapp1.0",
        "netcoreapp1.1",
        "netcoreapp2.0",
        "netcoreapp2.1",
        "netcoreapp2.2",
        "netcoreapp3.0",
        "netcoreapp3.1",
        "net5.0",
        "net6.0",
        "net7.0",
        "net8.0",
        "net9.0"
    ];

    private readonly List<NuGetFramework> _frameworks = tfms.Select(f => NuGetFramework.ParseFolder(f)).ToList();
}