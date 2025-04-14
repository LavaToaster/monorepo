load("@rules_dotnet//dotnet:defs.bzl", "nuget_repo", "nuget_archive")

_GLOBAL_NUGET_PREFIX = "nuget"

def _deps_select_statment(ctx, deps):
    if len(deps) == 0:
        return "\"//conditions:default\": []"

    return ",".join(["\n    \"@rules_dotnet//dotnet:tfm_{tfm}\": [{deps_list}]".format(tfm = tfm, deps_list = ",".join(["\"@{nuget_repo_name}//{dep_name}\"".format(dep_name = d.lower(), nuget_repo_name = ctx.attr.repo_name.lower()) for d in tfm_deps])) for (tfm, tfm_deps) in deps.items()])

def _nuget_repo_impl(ctx):
    for package in ctx.attr.packages:
        package = json.decode(package)
        name = package["name"]
        id = package["id"]
        version = package["version"]
        sha512 = package["sha512"]
        deps = package["dependencies"]

        targeting_pack_overrides = ctx.attr.targeting_pack_overrides["{}|{}".format(id.lower(), version)]
        framework_list = ctx.attr.framework_list["{}|{}".format(id.lower(), version)]

        ctx.template("{}/{}/BUILD.bazel".format(id.lower(), version), ctx.attr._template, {
            "{PREFIX}": _GLOBAL_NUGET_PREFIX,
            "{ID}": id,
            "{ID_LOWER}": id.lower(),
            "{VERSION}": version,
            "{DEPS}": _deps_select_statment(ctx, deps),
            "{TARGETING_PACK_OVERRIDES}": json.encode({override.lower().split("|")[0]: override.lower().split("|")[1] for override in targeting_pack_overrides}),
            "{FRAMEWORK_LIST}": json.encode({override.lower().split("|")[0]: override.lower().split("|")[1] for override in framework_list}),
            "{SHA_512}": sha512,
        })

        # currently we only support one version of a package
        ctx.file("{}/BUILD.bazel".format(name.lower()), r"""package(default_visibility = ["//visibility:public"])
alias(name = "{name}", actual = "//{id}/{version}")
alias(name = "content_files", actual = "@{prefix}.{id}.v{version}//:content_files")
alias(name = "files", actual = "@{prefix}.{id}.v{version}//:files")
""".format(prefix = _GLOBAL_NUGET_PREFIX, name = name.lower(), id = id.lower(), version = version))

_nuget_repo = repository_rule(
    _nuget_repo_impl,
    attrs = {
        "repo_name": attr.string(
            mandatory = True,
            doc = "The apparent name of the repo. This is needed because in bzlmod, the name attribute becomes the canonical name.",
        ),
        "packages": attr.string_list(
            mandatory = True,
            allow_empty = False,
        ),
        "targeting_pack_overrides": attr.string_list_dict(
            allow_empty = True,
            default = {},
        ),
        "framework_list": attr.string_list_dict(
            allow_empty = True,
            default = {},
        ),
        "_template": attr.label(
            default = "//prelude/dotnet:template.BUILD",
        ),
    },
)

def _nuget_packages_impl(module_ctx):
    """
    Implementation of the nuget_packages extension.

    Args:
        module_ctx: The module context
    """

    repo_names = []
    imported_packages = set()

    for module in module_ctx.modules:
        for from_file_tag in module.tags.from_file:
            nuget_deps_file = from_file_tag.nuget_deps
            
            # Load json file
            deps_string = module_ctx.read(nuget_deps_file)
            deps = json.decode(deps_string)   

            for repo_name, packages in deps.items():
                repo_names.append(repo_name)
            
                for package in packages:
                    id = package["id"].lower()
                    version = package["version"].lower()
                    package_name = "{}.{}.v{}".format(_GLOBAL_NUGET_PREFIX, id, version)

                    # Check if the package is already imported
                    if package_name in imported_packages:
                        continue

                    imported_packages.add(package_name)

                    nuget_archive(
                        name = package_name,
                        sources = package["sources"],
                        netrc = package.get("netrc", None),
                        id = id,
                        version = version,
                        sha512 = package["sha512"],
                    )
                    

                _nuget_repo(
                    name = repo_name,
                    repo_name = repo_name,
                    packages = [json.encode(package) for package in packages],
                    targeting_pack_overrides = {"{}|{}".format(package["id"].lower(), package["version"]): package["targeting_pack_overrides"] for package in packages},
                    framework_list = {"{}|{}".format(package["id"].lower(), package["version"]): package["framework_list"] for package in packages},
                )
    
    return module_ctx.extension_metadata(
        root_module_direct_deps = repo_names,
        root_module_direct_dev_deps = [],
        # reproducible = True,
    )

_from_file_tag = tag_class(
    doc = """
    Imports Nuget packages from a nuget.deps.json file. Generated by the nuget2bazel
    tool.

    All direct and transitive dependencies of the NuGet packages are imported.
    """,
    attrs = {
        "nuget_deps" : attr.label(
            doc = "Path to the generated nuget deps file",
            mandatory = True,
        )
    },
)

nuget_packages_extension = module_extension(
    implementation = _nuget_packages_impl,
    tag_classes = {
        "from_file": _from_file_tag,
    }
)