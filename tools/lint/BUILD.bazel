"""Definition of linter tools

This is in its own package because it has so many loading-time symbols,
we don't want to trigger eager fetches of these for builds which aren't running linters.
"""

load("@rules_go//go:def.bzl", "TOOLS_NOGO", "nogo")

package(default_visibility = ["//:__subpackages__"])

nogo(
    name = "nogo",
    visibility = ["//visibility:public"],
    deps = TOOLS_NOGO,
)
