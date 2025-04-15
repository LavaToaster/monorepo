"""Macro for creating multi-platform dotnet OCI images."""

load("@rules_dotnet//dotnet:defs.bzl", "publish_binary")
load("@rules_oci//oci:defs.bzl", "oci_image", "oci_image_index", "oci_load")
load("@aspect_bazel_lib//lib:tar.bzl", "tar", "mtree_spec", "mtree_mutate")

def dotnet_oci_image(
        name,
        binary,
        app_name,
        target_framework = "net8.0",
        image_repo_name = None,
        base_image = "@aspnet_chiseled",
        platforms = ["linux_amd64", "linux_arm64"]):
    """Creates dotnet publish and OCI image targets for multiple platforms.
    
    This macro simplifies the process of creating multi-platform dotnet containerized applications
    by generating all the necessary publish, tar, mtree, and OCI image targets.
    
    Args:
        name: Base name for the generated targets
        binary: The dotnet binary target to publish
        app_name: Name of the application binary (used for entrypoint)
        target_framework: The .NET target framework (e.g., "net8.0")
        image_repo_name: Repository name for the image (defaults to binary name if not specified)
        base_image: Base image name without platform suffix. Platform will be appended automatically.
        platforms: List of platforms to build for (e.g., ["linux_amd64", "linux_arm64"])
    """
    
    if not image_repo_name:
        image_repo_name = app_name.lower()
    
    runtime_identifiers = {
        "linux_amd64": "linux-x64",
        "linux_arm64": "linux-arm64",
    }
    
    image_targets = []
    
    for platform in platforms:
        if platform not in runtime_identifiers:
            fail("Platform %s not supported. Supported platforms: %s" % (platform, runtime_identifiers.keys()))
        
        runtime_identifier = runtime_identifiers[platform]
        platform_base_image = base_image + "_" + platform
        
        publish_name = "%s_publish_%s" % (name, platform)
        publish_binary(
            name = publish_name,
            binary = binary,
            target_framework = target_framework,
            runtime_identifier = runtime_identifier,
        )
        
        mtree_name = "%s_mtree_%s" % (name, platform)
        mtree_spec(
            name = mtree_name,
            srcs = [":%s" % publish_name],
        )
        
        restructure_name = "%s_restructure_%s" % (name, platform)
        mtree_mutate(
            name = restructure_name,
            strip_prefix = "%s/%s_publish_%s/publish/%s" % (native.package_name(), name, platform, runtime_identifier),
            package_dir = "app",
            mtree = ":%s" % mtree_name,
        )
        
        layer_name = "%s_image_layer_%s" % (name, platform)
        tar(
            name = layer_name,
            srcs = [":%s" % publish_name],
            mtree = ":%s" % restructure_name,
        )
        
        image_name = "%s_image_%s" % (name, platform)
        oci_image(
            name = image_name,
            base = platform_base_image,
            entrypoint = ["./%s" % app_name],
            workdir = "/app",
            tars = [":%s" % layer_name],
        )
        
        oci_load(
            name = "%s_load_%s" % (name, platform),
            image = ":%s" % image_name,
            repo_tags = ["%s:latest" % image_repo_name],
        )
        
        image_targets.append(":%s" % image_name)
    
    oci_image_index(
        name = name,
        images = image_targets,
    )
    
    oci_load(
        name = "%s_load" % name,
        image = ":%s" % name,
        repo_tags = ["%s:latest" % image_repo_name],
    )