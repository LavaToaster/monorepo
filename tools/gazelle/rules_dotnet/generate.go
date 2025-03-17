package rules_dotnet

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"github.com/bazelbuild/bazel-gazelle/language"
	"github.com/bazelbuild/bazel-gazelle/resolve"
	"github.com/bazelbuild/bazel-gazelle/rule"
)

// GenerateRules generates rules for C# build files.
func (l *csharpLang) GenerateRules(args language.GenerateArgs) language.GenerateResult {
	var result language.GenerateResult

	// path := args.Dir
	// fmt.Println("--------------------")
	// fmt.Println("Checking directory: ", path)

	l.addProjectRules(args, &result)

	return result
}

func (l *csharpLang) addProjectRules(args language.GenerateArgs, result *language.GenerateResult) {
	// Check if we are in a directory that contains a .csproj file\
	csProjFile := ""
	packagesJsonFile := ""

	for _, f := range args.RegularFiles {
		// fmt.Printf("File: %s/%s\n", path, f)

		if strings.HasSuffix(f, ".csproj") {
			csProjFile = fmt.Sprintf("%s/%s", args.Dir, f)
		}

		if strings.HasSuffix(f, "packages.lock.json") {
			packagesJsonFile = fmt.Sprintf("%s/%s", args.Dir, f)
		}
	}

	if csProjFile == "" {
		return
	}

	fmt.Printf("Found .csproj file: %s\n", csProjFile)

	// Parse the .csproj file
	csProj, err := ParseCSProject(csProjFile)
	if err != nil {
		fmt.Println("Error parsing .csproj file: ", err)
		return
	}

	projectDeps := []string{}

	if packagesJsonFile != "" {
		// get workspace root
		root := os.Getenv("BUILD_WORKSPACE_DIRECTORY")

		results := DiscoverNuGetPackages(root+"/tools/gazelle/rules_dotnet/nuget_query", packagesJsonFile)

		if results.Error != nil {
			fmt.Println("Error discovering NuGet packages: ", results.Error)
			return
		}

		for _, pkg := range results.Result.BazelNugetPackages {
			key := pkg.Id + ":" + pkg.Version
			l.Packages[key] = pkg
		}

		for _, dep := range results.Result.DirectDependencies {
			depName := dep[0]
			// TODO: Do we need multiple versions?
			// depVersion := dep[1]

			projectDeps = append(projectDeps, "@nuget//"+strings.ToLower(depName))
		}

		for _, lib := range csProj.References {
			projectDir := filepath.Dir(csProjFile)
			normalized := NormalizeCsProjPath(projectDir, lib)

			bazelTarget := strings.ReplaceAll(filepath.Dir(normalized), root+"/", "")

			projectDeps = append(projectDeps, "//"+bazelTarget)
		}
	}

	// Collect all .cs files in the directory
	csharpFiles := []string{}
	for _, f := range args.RegularFiles {
		if strings.HasSuffix(f, ".cs") {
			csharpFiles = append(csharpFiles, f)
		}
	}

	// Also look in subdirectories if needed
	for _, d := range args.Subdirs {
		// Skip hidden directories and vendor directories
		if strings.HasPrefix(d, ".") || d == "vendor" || d == "node_modules" {
			continue
		}

		dirPath := filepath.Join(args.Dir, d)
		files, err := filepath.Glob(filepath.Join(dirPath, "*.cs"))
		if err == nil {
			for _, f := range files {
				// Make paths relative to the current directory
				relPath, err := filepath.Rel(args.Dir, f)
				if err == nil {
					csharpFiles = append(csharpFiles, relPath)
				}
			}
		}
	}

	// for _, d := range csharpFiles {
	// 	fmt.Println("File: ", d)
	// }

	// Generate the csharp_library rule
	if csProj.ProjectType == Library {
		ruleName := filepath.Base(args.Dir)

		r := rule.NewRule("csharp_library", ruleName)

		// result.Imports = append(result.Imports, types.)

		// Add basic attributes
		r.SetAttr("srcs", csharpFiles)
		r.SetAttr("deps", projectDeps)
		r.SetAttr("visibility", []string{"//visibility:public"})

		// Add additional attributes if needed
		if len(csProj.TargetFramework) > 0 {
			r.SetAttr("target_frameworks", []string{csProj.TargetFramework})
		}

		// Add the rule to the result
		result.Gen = append(result.Gen, r)

		importSpec := resolve.ImportSpec{
			Lang: languageName,
			// I'm not sure what this is for, so I've put in something silly
			// to make it obvious when it appears in an output
			Imp: fmt.Sprintf("//%s:REMEMBER_THIS_IS_IMPORTANT%s", args.Rel, ruleName),
		}

		result.Imports = append(result.Imports, importSpec)

		fmt.Printf("Generated dotnet_library rule: %s\n", ruleName)
	}

	if csProj.ProjectType == Binary {
		// Determine the rule name from the project name or file name
		ruleName := filepath.Base(args.Dir)

		r := rule.NewRule("csharp_binary", ruleName)

		r.SetAttr("srcs", csharpFiles)
		r.SetAttr("deps", projectDeps)

		if strings.EqualFold(csProj.Sdk, "Microsoft.NET.Sdk.Web") {
			r.SetAttr("project_sdk", "web")
		}

		if len(csProj.TargetFramework) > 0 {
			r.SetAttr("target_frameworks", []string{csProj.TargetFramework})
		}

		result.Gen = append(result.Gen, r)

		importSpec := resolve.ImportSpec{
			Lang: languageName,
			Imp:  fmt.Sprintf("//%s:REMEMBER_THIS_IS_IMPORTANT%s", args.Rel, ruleName),
		}

		result.Imports = append(result.Imports, importSpec)

		fmt.Printf("Generated dotnet_binary rule: %s\n", ruleName)
	}

	fmt.Println("")
}

func isFrameworkSupported(framework string, tfms []string) bool {
	for _, tfm := range tfms {
		if tfm == framework {
			return true
		}
	}

	return false
}

func contains(arr []string, val string) bool {
	for _, a := range arr {
		if a == val {
			return true
		}
	}

	return false
}
