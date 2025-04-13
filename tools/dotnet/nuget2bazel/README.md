# nuget2bazel

A tool to convert NuGet package dependencies to Bazel build definitions.

## Overview

nuget2bazel is a utility that analyzes NuGet package lock files (`packages.lock.json`) in a workspace, downloads package metadata, and generates a Bazel build file (`nuget_deps.bzl`) that defines all NuGet package dependencies for use with rules_dotnet.

## Features

- Scans a workspace for all `packages.lock.json` files to discover dependencies
- Automatically downloads package information from NuGet sources
- Extracts package metadata including dependencies, framework compatibility information, and hashes
- Generates properly formatted Bazel build files
- Handles dependencies between packages
- Supports all modern .NET target frameworks

## Usage

### Command Line Options

- `--workspace-root`: Path to the workspace root directory (defaults to BUILD_WORKSPACE_DIRECTORY env var)
- `--output-file`: Path to output file (defaults to nuget_deps.bzl in workspace root)
- `--package-source`: NuGet package source URL (defaults to nuget.org)

### Examples

Basic usage within a Bazel environment:
```bash
bazel run //tools/dotnet/nuget2bazel
```

Specifying custom options:
```bash
bazel run //tools/dotnet/nuget2bazel -- --workspace-root=/path/to/workspace --output-file=/path/to/output.bzl
```

## Requirements

- .NET 8.0 SDK
- Access to NuGet package repositories
