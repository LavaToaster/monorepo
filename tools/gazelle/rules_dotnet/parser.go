package rules_dotnet

import (
	"encoding/xml"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strings"
)

// CSProjectType represents the type of C# project
type CSProjectType int

const (
	// Library is a standard C# library
	Library CSProjectType = iota
	// Binary is an executable C# application
	Binary
	// Test is a generic test project
	Test
	// NUnitTest is a test project using NUnit
	NUnitTest
)

func (pt CSProjectType) String() string {
	switch pt {
	case Library:
		return "csharp_library"
	case Binary:
		return "csharp_binary"
	case Test:
		return "csharp_test"
	case NUnitTest:
		return "csharp_nunit_test"
	default:
		return "unknown"
	}
}

// CSProject represents a parsed .csproj file
type CSProject struct {
	FilePath        string
	Name            string
	ProjectType     CSProjectType
	SourceFiles     []string
	References      []string
	TargetFramework string
	Sdk             string
}

// CSProjectXML represents the XML structure of a .csproj file
type CSProjectXML struct {
	XMLName        xml.Name        `xml:"Project"`
	Sdk            string          `xml:"Sdk,attr"`
	PropertyGroups []PropertyGroup `xml:"PropertyGroup"`
	ItemGroups     []ItemGroup     `xml:"ItemGroup"`
}

// PropertyGroup represents a PropertyGroup in the .csproj file
type PropertyGroup struct {
	OutputType      string `xml:"OutputType"`
	TargetFramework string `xml:"TargetFramework"`
	IsTestProject   string `xml:"IsTestProject"`
}

// ItemGroup represents an ItemGroup in the .csproj file
type ItemGroup struct {
	Compiles          []Compile          `xml:"Compile"`
	PackageReferences []PackageReference `xml:"PackageReference"`
	ProjectReferences []ProjectReference `xml:"ProjectReference"`
}

// Compile represents a source file in the .csproj file
type Compile struct {
	Include string `xml:"Include,attr"`
}

// PackageReference represents a NuGet package reference
type PackageReference struct {
	Include string `xml:"Include,attr"`
	Version string `xml:"Version,attr"`
}

// ProjectReference represents a reference to another project
type ProjectReference struct {
	Include string `xml:"Include,attr"`
}

// ParseCSProject parses a .csproj file and determines its type
func ParseCSProject(filePath string) (*CSProject, error) {
	data, err := os.ReadFile(filePath)
	if err != nil {
		return nil, fmt.Errorf("failed to read csproj file: %v", err)
	}

	var proj CSProjectXML
	if err := xml.Unmarshal(data, &proj); err != nil {
		return nil, fmt.Errorf("failed to parse csproj XML: %v", err)
	}

	csProj := &CSProject{
		FilePath:    filePath,
		Name:        strings.TrimSuffix(filepath.Base(filePath), ".csproj"),
		ProjectType: Library,
		SourceFiles: []string{},
		References:  []string{},
		Sdk:         proj.Sdk,
	}

	isTestProject := false
	hasNUnitDependency := false

	for _, pg := range proj.PropertyGroups {
		if strings.EqualFold(pg.OutputType, "exe") {
			csProj.ProjectType = Binary
		}

		if strings.EqualFold(pg.IsTestProject, "true") {
			isTestProject = true
		}

		if pg.TargetFramework != "" {
			csProj.TargetFramework = pg.TargetFramework
		}
	}

	if csProj.TargetFramework == "" {
		return nil, fmt.Errorf("failed to determine target framework for %s", filePath)
	}

	// Check for NUnit dependencies in all ItemGroups
	for _, itemGroup := range proj.ItemGroups {
		for _, pkgRef := range itemGroup.PackageReferences {
			if strings.Contains(strings.ToLower(pkgRef.Include), "nunit") {
				hasNUnitDependency = true
				break
			}
		}

		if hasNUnitDependency {
			break
		}
	}

	if isTestProject {
		if hasNUnitDependency {
			csProj.ProjectType = NUnitTest
		} else {
			csProj.ProjectType = Test
		}
	}

	for _, itemGroup := range proj.ItemGroups {
		for _, compile := range itemGroup.Compiles {
			csProj.SourceFiles = append(csProj.SourceFiles, compile.Include)
		}
	}

	for _, itemGroup := range proj.ItemGroups {
		for _, projRef := range itemGroup.ProjectReferences {
			csProj.References = append(csProj.References, projRef.Include)
		}
	}

	return csProj, nil
}

// DetermineRuleType analyzes a csproj file and returns the appropriate Bazel rule type
func DetermineRuleType(csprojPath string) (string, error) {
	proj, err := ParseCSProject(csprojPath)
	if err != nil {
		return "", err
	}

	ruleType := proj.ProjectType.String()
	log.Printf("Determined rule type for %s: %s", csprojPath, ruleType)
	return ruleType, nil
}

func NormalizeCsProjPath(baseDir, relativePath string) string {
	// Convert backslashes to forward slashes for consistency
	baseDir = strings.ReplaceAll(baseDir, "\\", "/")
	relativePath = strings.ReplaceAll(relativePath, "\\", "/")

	// Join the paths and clean (removes ".." and "." elements)
	fullPath := filepath.Clean(filepath.Join(baseDir, relativePath))

	return fullPath
}
