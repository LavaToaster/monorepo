"""
Macros for generating globalusings.cs files in .NET projects based on parsed XML data from the .NET toolchain.
"""

# Dictionary of SDK types to their default global using namespaces
# This list has been taken from https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview#implicit-using-directives
_SDK_DEFAULT_USINGS = {
    "Microsoft.NET.Sdk": [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks",
    ],
    "Microsoft.NET.Sdk.Web": [
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
    "Microsoft.NET.Sdk.Worker": [
        # Additional Worker SDK namespaces
        "Microsoft.Extensions.Configuration",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Hosting",
        "Microsoft.Extensions.Logging",
    ],
}
def _format_using(include, alias = None, static = False):
    """
    Formats a using directive with optional alias and static modifier.
    
    Args:
        include: The namespace to include.
        alias: Optional alias for the namespace.
        static: Whether the using should be static.
        
    Returns:
        A properly formatted using directive string.
    """
    parts = []

    if static and alias != None:
        fail("Cannot use static and alias at the same time.")
    
    if alias:
        parts.append("{} =".format(alias))

    if static:
        parts.append("static")
    
    parts.append(include)
    
    return "global using {};".format(" ".join(parts))

def _process_usings(usings_list):
    """
    Processes a list of using directives which can be strings or dictionaries.
    
    Args:
        usings_list: List of using directives. Each item can be:
            - String: simple namespace
            - Dict: {
                "include": "Namespace.To.Use",
                "alias": "Optional.Alias",  # Optional
                "static": True/False        # Optional
              }
              
    Returns:
        List of formatted using directive strings.
    """
    result = []
    
    for using in usings_list:
        if type(using) == "string":
            result.append(_format_using(using))
        elif type(using) == "dict":
            result.append(_format_using(
                include = using["include"],
                alias = using.get("alias"),
                static = using.get("static", False)
            ))
        
    return result

def csharp_globalusings(name, sdk = None, usings = None):
    """
    Generates a globalusings.cs file with the appropriate using directives based on SDK type.
    
    Args:
        name: Name of the rule.
        sdk: The .NET SDK type (e.g., "Microsoft.NET.Sdk", "Microsoft.NET.Sdk.Web").
        usings: Optional list of additional namespaces to include.
    """
    # Start with the SDK-specific default using directives
    all_usings = list(_SDK_DEFAULT_USINGS.get(sdk, []))

    # If SDK is specialized, add the base SDK usings first
    if sdk and sdk.startswith("Microsoft.NET.Sdk."):
        all_usings = _SDK_DEFAULT_USINGS["Microsoft.NET.Sdk"] + all_usings
    
    # Add user-specified usings if provided
    if usings:
        all_usings.extend(usings)
    
    if len(all_usings) == 0:
        fail("No using directives provided or found: {}".format(sdk))
    
    # Generate the file content with formatted using directives
    file_content = "\n".join(_process_usings(all_usings))
    

    native.genrule(
        name = name,
        srcs = [],
        outs = ["GlobalUsings.cs"],
        cmd = "echo '{content}' > $@".format(content = file_content),
    )