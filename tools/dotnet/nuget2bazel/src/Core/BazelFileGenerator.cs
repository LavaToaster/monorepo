using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using nuget2bazel.Models;

namespace nuget2bazel.Core;

/// <summary>
/// Generates a file that bazel can use to load NuGet packages.
/// </summary>
public static class BazelFileGenerator
{    
    /// <summary>
    /// Generates a file that bazel can use to load NuGet packages.
    /// </summary>
    /// <param name="packages">The list of NuGet packages.</param>
    /// <returns>the JSON string representing the NuGet packages.</returns>
    public static string Generate(SortedDictionary<string, List<RulesDotnetNugetPackage>> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);

        // Marshal the packages to JSON with nice formatting
        var packagesJson = JsonSerializer.Serialize(packages, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        });

        return packagesJson;
    }
}