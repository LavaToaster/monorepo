// See https://aka.ms/new-console-template for more information

using System;
using System.CommandLine;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using nuget_query;
using nuget_query.Models;

var fileOption = new Option<FileInfo>(
    name: "--lock-file",
    description: "Path to packages.lock.json files"
)
{
    IsRequired = true
};

var rootCommand = new RootCommand("NuGet package analyzer for Bazel");
rootCommand.AddOption(fileOption);

rootCommand.SetHandler(async (file) =>
{
    var packageAnalyzer = new PackageAnalyzer(file.FullName);
    var bazelNugetPackages = await packageAnalyzer.GetBazelNugetPackages();
    var programOutput = new ProgramOutput(packageAnalyzer.DirectDependencies, bazelNugetPackages);
    
    var outputJson = JsonSerializer.Serialize(programOutput, new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    });
    
    Console.WriteLine(outputJson);
}, fileOption);

await rootCommand.InvokeAsync(args);