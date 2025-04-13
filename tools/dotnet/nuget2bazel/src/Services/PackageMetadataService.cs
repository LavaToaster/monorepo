using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.ProjectModel;

namespace nuget2bazel.Services;

/// <summary>
/// Provides services for extracting metadata from NuGet packages.
/// </summary>
public class PackageMetadataService
{
    private readonly PackagesLockFile _packagesLockFile;
    private readonly IReadOnlyList<NuGetFramework> _supportedFrameworks;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageMetadataService"/> class.
    /// </summary>
    /// <param name="lockFilePath">The path to the packages.lock.json file.</param>
    /// <param name="supportedFrameworks">The supported .NET frameworks.</param>
    public PackageMetadataService(string lockFilePath, IReadOnlyList<NuGetFramework> supportedFrameworks)
    {
        if (string.IsNullOrEmpty(lockFilePath))
            throw new ArgumentNullException(nameof(lockFilePath));
        
        _packagesLockFile = PackagesLockFileFormat.Read(lockFilePath);
        _supportedFrameworks = supportedFrameworks ?? throw new ArgumentNullException(nameof(supportedFrameworks));
    }

    /// <summary>
    /// Gets the targeting pack overrides from a package.
    /// </summary>
    /// <param name="packageReader">The package reader.</param>
    /// <returns>An array of targeting pack overrides.</returns>
    public string[] GetTargetingPackOverrides(PackageReaderBase packageReader)
    {
        if (packageReader == null)
            throw new ArgumentNullException(nameof(packageReader));
            
        var overridePath = packageReader.GetItems("data")
            .SelectMany(f => f.Items)
            .FirstOrDefault(f => f.EndsWith("PackageOverrides.txt"));

        if (overridePath == null) 
            return Array.Empty<string>();
        
        var file = packageReader.GetStream(overridePath);
        using var reader = new StreamReader(file);
        return reader.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }
    
    /// <summary>
    /// Gets the framework list from a package.
    /// </summary>
    /// <param name="packageReader">The package reader.</param>
    /// <returns>An array of framework list entries.</returns>
    public string[] GetFrameworkList(PackageReaderBase packageReader)
    {
        if (packageReader == null)
            throw new ArgumentNullException(nameof(packageReader));
            
        var frameworkListPath = packageReader.GetItems("data")
            .SelectMany(f => f.Items)
            .FirstOrDefault(f => f.EndsWith("FrameworkList.xml"));

        if (frameworkListPath == null) 
            return Array.Empty<string>();
        
        var file = packageReader.GetStream(frameworkListPath);
        var xmlDocument = new XmlDocument();
        xmlDocument.Load(file);
            
        var root = xmlDocument.DocumentElement;
        var managedAssemblies = new List<string>();
            
        foreach (XmlNode node in root!.ChildNodes)
        {
            if (node.Attributes?["Type"]?.Value != "Managed") 
                continue;

            var name = node.Attributes["AssemblyName"]?.Value;
            var version = node.Attributes["AssemblyVersion"]?.Value;
                    
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(version))
            {
                managedAssemblies.Add($"{name}|{version}");
            }
        }
            
        return managedAssemblies.ToArray();
    }

    /// <summary>
    /// Gets the dependencies per target framework from a package.
    /// </summary>
    /// <param name="packageReader">The package reader.</param>
    /// <returns>A dictionary mapping target framework names to their dependencies.</returns>
    public Dictionary<string, List<string>> GetDependenciesPerTargetFramework(PackageReaderBase packageReader)
    {
        if (packageReader == null)
            throw new ArgumentNullException(nameof(packageReader));
            
        var frameworkReducer = new FrameworkReducer();
        var packageDependencies = packageReader.GetPackageDependencies();
        var dependenciesByFramework = new Dictionary<string, List<string>>();

        foreach (var tfm in _supportedFrameworks)
        {
            var nearest = frameworkReducer.GetNearest(tfm, packageDependencies.Select(x => x.TargetFramework));
            var frameworkDependencies = packageDependencies.Where(x => x.TargetFramework.Equals(nearest)).ToList();

            var shortFrameworkName = tfm.GetShortFolderName();
            dependenciesByFramework[shortFrameworkName] = new List<string>();

            foreach (var pkg in frameworkDependencies.SelectMany(fwd => fwd.Packages))
            {
                // Only include dependencies that exist in the lock file
                var exists = _packagesLockFile.Targets.SelectMany(t => t.Dependencies)
                    .Any(d => d.Id.Equals(pkg.Id, StringComparison.OrdinalIgnoreCase));

                if (exists) 
                {
                    dependenciesByFramework[shortFrameworkName].Add(pkg.Id);
                }
            }
        }

        return dependenciesByFramework;
    }
}