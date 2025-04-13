package dotnet

import "github.com/bazelbuild/bazel-gazelle/rule"

// File constants
const (
	// PackagesLockFileName is the filename for NuGet package lock files
	// not to be confused with package-lock.json used by npm
	PackagesLockFileName = "packages.lock.json"
)

// Rule kind constants
const (
	CSharpLibraryKind         = "csharp_library"
	CSharpBinaryKind          = "csharp_binary"
	CSharpTestKind            = "csharp_test"
	CSharpNUnitTestKind       = "csharp_nunit_test"
	RulesDotnetModuleName     = "rules_dotnet"
	RulesDotnetRepositoryName = RulesDotnetModuleName
)

func (*dotnetLang) Kinds() map[string]rule.KindInfo {
	return dotnetKinds
}

var dotnetKinds = map[string]rule.KindInfo{
	CSharpLibraryKind: {
		MatchAny: false,
		NonEmptyAttrs: map[string]bool{
			"srcs":              true,
			"target_frameworks": true,
		},
		SubstituteAttrs: map[string]bool{},
		MergeableAttrs: map[string]bool{
			"srcs": true,
		},
		ResolveAttrs: map[string]bool{
			"deps": true,
		},
	},
	CSharpBinaryKind: {
		MatchAny: false,
		NonEmptyAttrs: map[string]bool{
			"srcs":              true,
			"target_frameworks": true,
		},
		SubstituteAttrs: map[string]bool{},
		MergeableAttrs: map[string]bool{
			"srcs": true,
		},
		ResolveAttrs: map[string]bool{
			"deps": true,
		},
	},
	CSharpTestKind: {
		MatchAny: false,
		NonEmptyAttrs: map[string]bool{
			"srcs":              true,
			"target_frameworks": true,
		},
		SubstituteAttrs: map[string]bool{},
		MergeableAttrs: map[string]bool{
			"srcs": true,
		},
		ResolveAttrs: map[string]bool{
			"deps": true,
		},
	},
	CSharpNUnitTestKind: {
		MatchAny: false,
		NonEmptyAttrs: map[string]bool{
			"srcs":              true,
			"target_frameworks": true,
		},
		SubstituteAttrs: map[string]bool{},
		MergeableAttrs: map[string]bool{
			"srcs": true,
		},
		ResolveAttrs: map[string]bool{
			"deps": true,
		},
	},
}

func (*dotnetLang) ApparentLoads(moduleToApparentName func(string) string) []rule.LoadInfo {
	rulesDotnetName := moduleToApparentName(RulesDotnetModuleName)
	if rulesDotnetName == "" {
		rulesDotnetName = RulesDotnetRepositoryName
	}

	return []rule.LoadInfo{
		{
			Name: "@" + rulesDotnetName + "//dotnet:defs.bzl",
			Symbols: []string{
				CSharpBinaryKind,
				CSharpLibraryKind,
				CSharpTestKind,
				CSharpNUnitTestKind,
			},
		},
	}
}
