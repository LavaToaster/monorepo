#!/usr/bin/env bash 

set -e

bazel run //tools/dotnet/nuget2bazel
bazel run //:gazelle
bazel mod tidy