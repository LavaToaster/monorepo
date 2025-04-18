# Dotnet Gazelle

### Running Gazelle

[Gazelle](https://github.com/bazelbuild/bazel-gazelle) is a build file generator that automatically creates and updates Bazel build files based on source code. In this project, we have a custom Gazelle extension for handling .NET projects.

To run gazelle and update your BUILD files:

```bash
# Update all BUILD files in the repository
bazel run //:gazelle

# Update BUILD files in a specific directory
bazel run //:gazelle -- path/to/directory
```

The gazelle extension will scan your projects, detect dependencies, and generate or update the corresponding BUILD.bazel files.

### Managing NuGet Dependencies with nuget2bazel

NuGet dependencies are managed through a custom tool called `nuget2bazel`. This tool analyzes your project's packages.lock.json files and generates the necessary Bazel build definitions.

To update NuGet dependencies:

1. First, ensure your project has correct NuGet references in your .csproj files
2. Make sure each project has a packages.lock.json file (you can generate these ensuring you have `RestorePackagesWithLockFile` enabled and by running `dotnet restore`)
3. Run the nuget2bazel tool:

```bash
# Generate/update nuget.deps.json with all NuGet dependencies
bazel run //tools/dotnet/nuget2bazel

# Specify custom options if needed
bazel run //tools/dotnet/nuget2bazel -- --output-file=/path/to/output.json --package-source=https://your-nuget-source
```
4. Run `bazel mod tidy` to ensure that any new repos are added to your MODULE.bazel file

The tool will:
- Search for all packages.lock.json files in your workspace
- Download package metadata from NuGet
- Generate a nuget.deps.json file with all dependencies organized by project
- This file is referenced by the Bazel build system to resolve NuGet package dependencies

### Not Implemented:

- [ ] F#
- [ ] Private NuGet feeds
- [ ] Paket
- [ ] Resource Files Discovery
- [ ] All other attributes rules_dotnet supports on its rules
