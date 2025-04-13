using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;

namespace nuget2bazel.Utilities;

/// <summary>
/// Provides utility methods for working with .NET framework targeting information.
/// </summary>
public static class FrameworkUtilities
{
    /// <summary>
    /// Gets a list of .NET framework identifiers supported by this tool.
    /// </summary>
    public static IReadOnlyList<string> SupportedFrameworks => _supportedFrameworks;
    
    /// <summary>
    /// Gets a list of parsed NuGet frameworks for .NET frameworks supported by this tool.
    /// </summary>
    public static IReadOnlyList<NuGetFramework> ParsedFrameworks => 
        _parsedFrameworks ??= _supportedFrameworks.Select(NuGetFramework.ParseFolder).ToList();
        
    private static IReadOnlyList<NuGetFramework>? _parsedFrameworks;
    
    // List of supported .NET framework identifiers
    private static readonly IReadOnlyList<string> _supportedFrameworks = new List<string>
    {
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
    };
}