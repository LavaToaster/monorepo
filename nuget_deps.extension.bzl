"Generated"

load(":nuget_deps.bzl", "nuget_packages")

def _nuget_packages_impl(_ctx):
    nuget_packages()

nuget_packages_extension = module_extension(
    implementation = _nuget_packages_impl,
)