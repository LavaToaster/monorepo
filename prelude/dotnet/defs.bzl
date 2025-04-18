"Additional helpers for dotnet build rules"

load("oci.bzl", _dotnet_oci_image = "dotnet_oci_image")
load("globalusings.bzl", _csharp_globalusings = "csharp_globalusings")

csharp_globalusings = _csharp_globalusings
dotnet_oci_image = _dotnet_oci_image
