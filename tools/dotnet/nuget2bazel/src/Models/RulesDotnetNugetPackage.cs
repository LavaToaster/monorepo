using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace nuget2bazel.Models;

/// <summary>
/// Represents a NuGet package in Bazel format.
/// This model is serialized to JSON for use in Bazel build files.
/// </summary>
public class RulesDotnetNugetPackage
{
    /// <summary>
    /// Gets or sets the package identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    /// <summary>
    /// Gets or sets the package display name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; set; }
    
    /// <summary>
    /// Gets or sets the SHA-512 hash of the package.
    /// </summary>
    [JsonPropertyName("sha512")]
    public required string Sha512 { get; set; }
    
    /// <summary>
    /// Gets or sets the package sources where the package can be downloaded from.
    /// </summary>
    [JsonPropertyName("sources")]
    public required IReadOnlyList<string> Sources { get; set; }
    
    /// <summary>
    /// Gets or sets the package dependencies per target framework.
    /// </summary>
    [JsonPropertyName("dependencies")]
    public required Dictionary<string, List<string>> Dependencies { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the targeting pack overrides for this package.
    /// </summary>
    [JsonPropertyName("targeting_pack_overrides")]
    public required IReadOnlyList<string> TargetingPackOverrides { get; set; } = new List<string>();
    
    /// <summary>
    /// Gets or sets the framework list for this package.
    /// </summary>
    [JsonPropertyName("framework_list")]
    public required IReadOnlyList<string> FrameworkList { get; set; } = new List<string>();
    
    /// <summary>
    /// Creates a unique key for this package based on ID and version.
    /// </summary>
    /// <returns>A string in the format "id|version"</returns>
    public string GetUniqueKey() => $"{Id}|{Version}";
}