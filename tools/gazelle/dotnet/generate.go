package dotnet

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"github.com/bazelbuild/bazel-gazelle/language"
	"github.com/bazelbuild/bazel-gazelle/rule"
	"github.com/bazelbuild/buildtools/build"
	"github.com/bmatcuk/doublestar/v4"
	"github.com/emirpasic/gods/sets/treeset"
	parser "github.com/lavatoaster/dbot/tools/gazelle/dotnet/parser"
)

func (l *dotnetLang) GenerateRules(args language.GenerateArgs) language.GenerateResult {
	var result language.GenerateResult

	l.addProjectRules(args, &result)

	return result
}

func (l *dotnetLang) addProjectRules(args language.GenerateArgs, result *language.GenerateResult) {
	csProjFilePath, packagesLockPath, projectFilesErr := findProjectFiles(args)

	// Little too verbose for now
	logger.Debug("Checking directory", "dir", args.Dir)

	if projectFilesErr != nil {
		// Only log an error if it's not simply that no .csproj file was found
		if !strings.Contains(projectFilesErr.Error(), "no .csproj file found") {
			logger.Error("Cannot generate rules for project", "dir", args.Dir, "error", projectFilesErr)
		}
		return
	}

	logger.Info("Found dotnet project files", "path", csProjFilePath)

	projectFile, err := parser.ParseProjectFile(csProjFilePath)
	if err != nil {
		logger.Error("Error parsing .csproj file", "path", csProjFilePath, "error", err)
		return
	}

	lockFile, err := parser.ParsePackagesLockFile(packagesLockPath)
	if err != nil {
		logger.Error("Error parsing packages.lock.json file", "path", packagesLockPath, "error", err)
		return
	}

	projectType, err := identifyProjectType(projectFile)
	if err != nil {
		logger.Error("Error identifying project type", "path", csProjFilePath, "error", err)
		return
	}

	logger.Info("Identified project type", "type", projectType)

	ruleName := filepath.Base(args.Dir)

	dotnetPackageInfo := newDotnetPackageInfo()
	dotnetPackageInfo.project = &projectFile
	dotnetPackageInfo.lock = &lockFile

	var rules []rule.Rule
	var imports []interface{}

	dotnetRule := getOrCreateRule(args, ruleName, projectType)
	dotnetRule.SetPrivateAttr("projectFile", projectFile)
	dotnetRule.SetPrivateAttr("lockFile", lockFile)

	rules = append(rules, *dotnetRule)
	imports = append(imports, dotnetPackageInfo)

	sourceFiles, err := findSourceFiles(args.Dir)
	if err != nil {
		logger.Error("Error finding source files", "dir", args.Dir, "error", err)
		return
	}

	if sourceFiles.Size() > 0 {
		var sourceFilesList []string
		for _, file := range sourceFiles.Values() {
			dotnetPackageInfo.sources.Add(file)
			sourceFilesList = append(sourceFilesList, file.(string))
		}

		addGlobalUsings(args, &rules, &imports, dotnetRule, &sourceFilesList, projectFile)

		dotnetRule.SetAttr("srcs", sourceFilesList)
		dotnetRule.SetAttr("target_frameworks", []string{projectFile.ResolvedProps.TargetFramework})
	}

	if projectType == CSharpBinaryKind && projectFile.Sdk == "Microsoft.NET.Sdk.Web" {
		dotnetRule.SetAttr("project_sdk", "web")
		addAppsettingsFiles(dotnetRule)

		// TODO: Add publish rule
	}

	if projectType == CSharpLibraryKind {
		dotnetRule.SetAttr("visibility", []string{"//visibility:public"})
	}

	logger.Info("Resolved props", "props", projectFile.ResolvedProps)

	if projectFile.ResolvedProps.Nullable {
		dotnetRule.SetAttr("nullable", "enable")
	} else {
		dotnetRule.DelAttr("nullable")
	}

	for _, rule := range rules {
		result.Gen = append(result.Gen, &rule)
	}

	for _, imp := range imports {
		result.Imports = append(result.Imports, imp)
	}
}

// findFileWithExtension searches for a file in the regular files list with the given extension
// or with the exact name provided. Returns the full path to the file if found.
func findFileWithExtension(dir string, regularFiles []string, extension string, exactName string) (string, bool) {
	for _, f := range regularFiles {
		if (extension != "" && strings.HasSuffix(f, extension)) || (exactName != "" && f == exactName) {
			return filepath.Join(dir, f), true
		}
	}
	return "", false
}

func findProjectFiles(args language.GenerateArgs) (csProjPath string, packagesLockPath string, err error) {
	csProjPath, found := findFileWithExtension(args.Dir, args.RegularFiles, ".csproj", "")
	if !found {
		return "", "", fmt.Errorf("no .csproj file found in directory %s", args.Dir)
	}

	// Check if multiple .csproj files exist
	for _, f := range args.RegularFiles {
		if strings.HasSuffix(f, ".csproj") && filepath.Join(args.Dir, f) != csProjPath {
			return "", "", fmt.Errorf("multiple .csproj files found in directory %s", args.Dir)
		}
	}

	packagesLockPath, _ = findFileWithExtension(args.Dir, args.RegularFiles, "", PackagesLockFileName)

	return csProjPath, packagesLockPath, nil
}

func identifyProjectType(project parser.Project) (string, error) {
	// Check if the project is a test project
	isTestProject := false
	// Output type defaults to Library
	outputType := "Library"

	for _, propGroup := range project.PropertyGroups {
		if propGroup.IsTestProject() {
			isTestProject = true
			break
		}

		if propGroup.OutputType != "" {
			outputType = propGroup.OutputType
		}
	}

	if isTestProject {
		// Check for NUnit dependencies in all ItemGroups
		for _, itemGroup := range project.ItemGroups {
			for _, pkgRef := range itemGroup.PackageReferences {
				if strings.Contains(strings.ToLower(pkgRef.Include), "nunit") {
					return CSharpNUnitTestKind, nil
				}
			}
		}

		return CSharpTestKind, nil
	}

	if outputType == "Exe" || project.Sdk == "Microsoft.NET.Sdk.Web" {
		return CSharpBinaryKind, nil
	}

	if outputType == "Library" {
		return CSharpLibraryKind, nil
	}

	return "", fmt.Errorf("could not determine project type")
}

// findSourceFiles finds all the source files for a project
// excluding files in bin, obj, and node_modules directories
func findSourceFiles(dir string) (*treeset.Set, error) {
	sourceFiles := treeset.NewWithStringComparator()
	excludePatterns := []string{
		"**/{bin,obj,node_modules}/**",
	}

	searchGlob := fmt.Sprintf("%s/**/*.cs", dir)
	logger.Info("Searching for source files", "glob", searchGlob)

	basepath, pattern := doublestar.SplitPattern(searchGlob)
	fsys := os.DirFS(basepath)
	matches, err := doublestar.Glob(fsys, pattern)
	if err != nil {
		return nil, fmt.Errorf("error searching for source files: %w", err)
	}

matchLoop:
	for _, match := range matches {
		fullPath := filepath.Join(basepath, match)
		relPath, _ := filepath.Rel(dir, fullPath)

		for _, excludePattern := range excludePatterns {
			matched, _ := doublestar.Match(excludePattern, relPath)
			if matched {
				logger.Debug("Excluding file", "file", relPath, "pattern", excludePattern)
				continue matchLoop
			}
		}

		sourceFiles.Add(match)
	}

	logger.Info("Found source files", "count", sourceFiles.Size())
	for _, f := range sourceFiles.Values() {
		logger.Debug("Found source file", "file", f)
	}

	return sourceFiles, nil
}

func addAppsettingsFiles(r *rule.Rule) {
	// Create a list expression with the specific file
	filesList := &build.ListExpr{
		List: []build.Expr{
			&build.StringExpr{Value: "appsettings.json"},
		},
	}

	// Create a binary expression that adds the glob to the list
	plusExpr := &build.BinaryExpr{
		X:  filesList,
		Op: "+",
		Y: &build.CallExpr{
			X: &build.Ident{Name: "glob"},
			List: []build.Expr{
				&build.ListExpr{
					List: []build.Expr{
						&build.StringExpr{Value: "appsettings.*.json"},
					},
				},
			},
		},
	}

	// Set the attribute
	r.SetAttr("appsetting_files", plusExpr)
}

func addGlobalUsings(args language.GenerateArgs, rules *[]rule.Rule, imports *[]interface{}, dotnetRule *rule.Rule, sourceFilesList *[]string, projectFile parser.Project) {
	var allUsings []parser.Usings
	isImplicitUsings := projectFile.ResolvedProps.ImplicitUsings

	for _, itemGroup := range projectFile.ItemGroups {
		for _, usings := range itemGroup.Usings {
			allUsings = append(allUsings, usings)
		}
	}

	if len(allUsings) == 0 && !isImplicitUsings {
		return
	}

	globalUsingsRule := getOrCreateRule(args, dotnetRule.Name()+".GlobalUsings", CSharpGlobalUsings)

	if len(allUsings) > 0 {
		usingsList := []interface{}{}

		for _, usings := range allUsings {
			value := map[string]interface{}{
				"include": usings.Include,
			}

			if usings.Alias != "" {
				value["alias"] = usings.Alias
			}

			if usings.IsStatic() {
				value["static"] = true
			}

			usingsList = append(usingsList, value)
		}

		globalUsingsRule.SetAttr("usings", usingsList)
	}

	if isImplicitUsings {
		globalUsingsRule.SetAttr("sdk", projectFile.Sdk)
	} else {
		globalUsingsRule.DelAttr("sdk")
	}

	*rules = append(*rules, *globalUsingsRule)
	*sourceFilesList = append(*sourceFilesList, ":"+globalUsingsRule.Name())
	*imports = append(*imports, make(map[string]interface{})) // nothing to import?
}

func getOrCreateRule(args language.GenerateArgs, ruleName string, ruleKind string) *rule.Rule {
	if args.File != nil {
		for _, r := range args.File.Rules {
			if r.Kind() == ruleKind && r.Name() == ruleName {
				return r
			}
		}
	}

	dotnetRule := rule.NewRule(ruleKind, ruleName)

	return dotnetRule
}
