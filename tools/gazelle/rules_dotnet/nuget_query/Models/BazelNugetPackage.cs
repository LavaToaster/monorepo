using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace nuget_query.Models;

public class BazelNugetPackage
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("version")]
    public required string Version { get; set; }
    
    [JsonPropertyName("sha512")]
    public required string Sha512 { get; set; }
    
    [JsonPropertyName("sources")]
    public required string[] Sources { get; set; }
    
    [JsonPropertyName("dependencies")]
    public required Dictionary<string, List<string>> Dependencies { get; set; } = new();
    
    [JsonPropertyName("targeting_pack_overrides")]
    public required string[] TargetingPackOverrides { get; set; } = [];
    
    [JsonPropertyName("framework_list")]
    public required string[] FrameworkList { get; set; } = [];
}