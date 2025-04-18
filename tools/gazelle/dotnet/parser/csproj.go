package csproj

import (
	"encoding/xml"
	"fmt"
	"os"
	"strings"
)

type Project struct {
	XMLName        xml.Name           `xml:"Project"`
	Sdk            string             `xml:"Sdk,attr"`
	PropertyGroups []PropertyGroup    `xml:"PropertyGroup"`
	ItemGroups     []ItemGroup        `xml:"ItemGroup"`
	ResolvedProps  ResolvedProperties `xml:"ResolvedProperties"`
}

type ResolvedProperties struct {
	ImplicitUsings bool
	IsTestProject  bool
	Nullable       bool
	OutputType     string
	// Not 100% sure how msbuild/dotnet handles multiple of these set...
	// afaik it shouldn't happen in practice
	TargetFramework string
}

func (p *Project) resolveProps() ResolvedProperties {
	var props ResolvedProperties

	for _, propGroup := range p.PropertyGroups {
		if propGroup.IsImplicitUsings() {
			props.ImplicitUsings = true
		}

		if propGroup.IsTestProject() {
			props.IsTestProject = true
		}

		if propGroup.IsNullable() {
			props.Nullable = true
		}

		if propGroup.OutputType != "" {
			props.OutputType = propGroup.OutputType
		}

		if propGroup.TargetFramework != "" {
			props.TargetFramework = propGroup.TargetFramework
		}
	}

	p.ResolvedProps = props

	return props
}

type PropertyGroup struct {
	OutputType      string `xml:"OutputType"`
	TargetFramework string `xml:"TargetFramework"`
	TestProject     string `xml:"IsTestProject"`
	Nullable        string `xml:"Nullable"`
	ImplicitUsings  string `xml:"ImplicitUsings"`
}

func (p *PropertyGroup) IsTestProject() bool {
	return isEnabled(p.TestProject)
}

func (p *PropertyGroup) IsNullable() bool {
	return isEnabled(p.Nullable)
}

func (p *PropertyGroup) IsImplicitUsings() bool {
	return isEnabled(p.ImplicitUsings)
}

type ItemGroup struct {
	Compiles          []Compile          `xml:"Compile"`
	PackageReferences []PackageReference `xml:"PackageReference"`
	ProjectReferences []ProjectReference `xml:"ProjectReference"`
	Usings            []Usings           `xml:"Using"`
}

type Usings struct {
	Include string `xml:"Include,attr"`
	Alias   string `xml:"Alias,attr"`
	Static  string `xml:"Static,attr"`
}

func (u *Usings) IsStatic() bool {
	return isEnabled(u.Static)
}

type Compile struct {
	Include string `xml:"Include,attr"`
}

type PackageReference struct {
	Include string `xml:"Include,attr"`
	Version string `xml:"Version,attr"`
}

type ProjectReference struct {
	Include string `xml:"Include,attr"`
}

func ParseProjectFile(filePath string) (Project, error) {
	var project Project

	byteValue, err := os.ReadFile(filePath)
	if err != nil {
		return project, fmt.Errorf("error reading file: %v", err)
	}

	err = xml.Unmarshal(byteValue, &project)
	if err != nil {
		return project, fmt.Errorf("error unmarshalling XML: %v", err)
	}

	project.resolveProps()

	return project, nil
}

func isEnabled(value string) bool {
	text := strings.ToLower(value)
	return text == "true" || text == "enable"
}
