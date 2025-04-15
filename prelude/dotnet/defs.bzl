"Wrapper around rules_dotnet to provide a more user-friendly API for C# targets."

load(
    "@rules_dotnet//dotnet:defs.bzl",
    rules_csharp_binary = "csharp_binary",
)
load("oci.bzl", _dotnet_oci_image = "dotnet_oci_image")

DEFAULT_RID = "any"

SUPPORTED_NULLABLE_LEVELS = [
    "disable",
    "enable",
    "warnings",
    "annotations",
]

SUPPORTED_SDKS = [
    "default",
    "web",
]

NUGET_FRAMEWORK_BY_SUPPORTED_FRAMEWORK = {
    "net6.0": "net60",
    "net7.0": "net70",
    "net8.0": "net80",
}

def _create_global_usings_file(filename, namespaces):
    content = "\n".join(["global using %s;" % ns for ns in namespaces])
    native.genrule(
        name = filename.replace(".cs", ""),
        outs = [filename],
        cmd = "echo '" + content + "' > $@",
    )

def unique_list(items):
    """
    Generates a list of unique items from the provided list, preserving the order of their first appearance.

    Args:
        items (list): A list of items from which duplicates will be removed.

    Returns:
        list: A list containing only unique items from the original list.
    """
    seen = []
    for item in items:
        if item not in seen:
            seen.append(item)
    return seen

def resolve_implicit_usings(sdk, additional_usings):
    """
    Combines and sorts base .NET SDK namespaces with SDK-specific namespaces, eliminating duplicates.

    Args:
        sdk (str): The identifier for the specific .NET SDK version being used.
        additional_usings (list): Additional namespaces specified by the user to include globally.

    Returns:
        list: A sorted list of unique namespaces that combines base, SDK-specific, and additional usings.
    """

    microsoft_net_sdk_usings = [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks",
    ]

    sdk_specific_usings = {
        "web": [
            "System.Net.Http.Json",
            "Microsoft.AspNetCore.Builder",
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Http",
            "Microsoft.AspNetCore.Routing",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.Hosting",
            "Microsoft.Extensions.Logging",
        ],
    }

    # Combine the default .NET SDK usings with specific SDK usings, removing any duplicates
    combined_usings = microsoft_net_sdk_usings + sdk_specific_usings.get(sdk, [])
    unique_usings = unique_list(combined_usings + additional_usings)

    # Sort the final list of usings alphabetically and return
    return sorted(unique_usings)

def handle_global_usings(name, sdk, implicit_usings_enabled, global_usings, srcs):
    """
    Manages the creation and addition of a global usings file to the source list based on the provided configurations.

    Args:
        name (str): The base name of the target for which the global usings file is being created.
                   This name is used as the prefix for the generated global usings file.
        sdk (str): The identifier for the specific .NET SDK version being used. This influences
                   which SDK-specific namespaces are included by default when implicit usings are enabled.
        implicit_usings_enabled (bool): Flag to determine if SDK-specific usings should be automatically included.
                                        If True, combines SDK-specific usings with `global_usings`.
                                        If False, only the namespaces in `global_usings` are used.
        global_usings (list of str): A list of namespaces specified by the user that should be included globally
                                     across the project. This list is either combined with SDK-specific namespaces
                                     or used alone based on the `implicit_usings_enabled` flag.
        srcs (list of str): The list of source files for the project. This list is modified in-place to
                            include the newly created global usings file.

    Returns:
        None: The function modifies the `srcs` list in-place by appending the path to the newly created global usings file.
    """
    if implicit_usings_enabled or global_usings:
        if implicit_usings_enabled:
            # Combine SDK-specific usings with any additional global usings provided.
            resolved_usings = resolve_implicit_usings(sdk, global_usings)
        else:
            # Only use the global usings provided.
            resolved_usings = unique_list(global_usings)

        implicit_usings_file = name + "_global_usings.cs"
        _create_global_usings_file(implicit_usings_file, resolved_usings)
        srcs.append(implicit_usings_file)

def csharp_binary(
        name,
        srcs = [],
        nullable = "enable",
        sdk = "default",
        implicit_usings_enabled = True,
        treat_warnings_as_errors = True,
        global_usings = [],
        **kwargs):
    handle_global_usings(name, sdk, implicit_usings_enabled, global_usings, srcs)

    rules_csharp_binary(
        name = name,
        srcs = srcs,
        nullable = nullable,
        project_sdk = sdk,
        treat_warnings_as_errors = treat_warnings_as_errors,
        **kwargs
    )

dotnet_oci_image = _dotnet_oci_image
