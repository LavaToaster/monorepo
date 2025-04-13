package dotnet

import (
	"os"
	"path/filepath"
	"strings"

	"github.com/bazelbuild/bazel-gazelle/config"
	"github.com/bazelbuild/bazel-gazelle/label"
	"github.com/bazelbuild/bazel-gazelle/repo"
	"github.com/bazelbuild/bazel-gazelle/resolve"
	"github.com/bazelbuild/bazel-gazelle/rule"
	"github.com/emirpasic/gods/sets/treeset"
)

var workspaceRoot = os.Getenv("BUILD_WORKSPACE_DIRECTORY")

func (l *dotnetLang) Resolve(
	c *config.Config,
	ix *resolve.RuleIndex,
	rc *repo.RemoteCache,
	r *rule.Rule,
	importData interface{},
	from label.Label,
) {
	logger.Info("Resolving rule", "rule", r.Name(), "from", from)

	if packageInfo, isPackageInfo := importData.(*DotnetPackageInfo); isPackageInfo {
		deps := treeset.NewWithStringComparator()
		projectTargetFramework := packageInfo.project.TargetFramework()
		itemGroups := packageInfo.project.ItemGroups

		for _, itemGroup := range itemGroups {
			for _, packageReference := range itemGroup.PackageReferences {
				// Do we need to look up the version?
				version := packageInfo.lock.Dependencies[projectTargetFramework][packageReference.Include].Resolved
				target := "@nuget//" + strings.ToLower(packageReference.Include)

				deps.Add(target)

				logger.Debug("Found package reference", "name", packageReference.Include, "resolvedVersion", version, "target", target)
			}

			for _, projectReference := range itemGroup.ProjectReferences {
				bazelTarget := GetBazelTarget(from.Pkg, projectReference.Include)

				deps.Add(bazelTarget)

				logger.Debug("Found project reference", "name", projectReference.Include, "target", bazelTarget)
			}
		}

		if !deps.Empty() {
			r.SetAttr("deps", deps.Values())
		}
	} else {
		logger.Warn("Unknown import with no/unknown package info", "kind", r.Kind(), "pkg", from.Pkg, "name", r.Name())
	}
}

func GetBazelTarget(pkgDir, include string) string {
	// Convert backslashes to forward slashes for consistency
	include = strings.ReplaceAll(include, "\\", "/")

	// Join the paths and clean (removes ".." and "." elements)
	path := filepath.Clean(filepath.Join(pkgDir, include))

	// Return the name of the package
	path = filepath.Dir(path)

	return "//" + path
}
