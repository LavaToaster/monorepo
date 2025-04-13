using System;

namespace nuget2bazel.Models;

/// <summary>
/// Represents a direct dependency reference from a project to a NuGet package.
/// </summary>
public class DirectDependency : IEquatable<DirectDependency>, IComparable<DirectDependency>
{
    /// <summary>
    /// Gets the package identifier.
    /// </summary>
    public string PackageId { get; }
    
    /// <summary>
    /// Gets the package version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectDependency"/> class.
    /// </summary>
    /// <param name="packageId">The package identifier.</param>
    /// <param name="version">The package version.</param>
    public DirectDependency(string packageId, string version)
    {
        PackageId = packageId ?? throw new ArgumentNullException(nameof(packageId));
        Version = version ?? throw new ArgumentNullException(nameof(version));
    }

    /// <summary>
    /// Gets a unique key for this dependency based on package ID and version.
    /// </summary>
    /// <returns>A string in the format "id|version"</returns>
    public string GetUniqueKey() => $"{PackageId}|{Version}";
    
    /// <inheritdoc/>
    public bool Equals(DirectDependency? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(PackageId, other.PackageId, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is DirectDependency dependency && Equals(dependency);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(PackageId);
    }
    
    /// <inheritdoc/>
    public int CompareTo(DirectDependency? other)
    {
        if (other is null) return 1;
        return string.Compare(PackageId, other.PackageId, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Creates a DirectDependency from an array containing package ID and version.
    /// </summary>
    public static DirectDependency FromArray(string[] array)
    {
        if (array == null || array.Length < 2)
            throw new ArgumentException("Array must contain at least package ID and version", nameof(array));
            
        return new DirectDependency(array[0], array[1]);
    }
    
    /// <summary>
    /// Converts this DirectDependency to an array containing package ID and version.
    /// </summary>
    public string[] ToArray() => [PackageId, Version];
}