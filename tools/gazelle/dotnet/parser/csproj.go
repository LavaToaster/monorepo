package csproj

import (
	"encoding/xml"
	"fmt"
	"os"
)

type Project struct {
	XMLName        xml.Name        `xml:"Project"`
	Sdk            string          `xml:"Sdk,attr"`
	PropertyGroups []PropertyGroup `xml:"PropertyGroup"`
	ItemGroups     []ItemGroup     `xml:"ItemGroup"`
}

// Not 100% sure how msbuild/dotnet handles multiple of these set...
// afaik it shouldn't happen in practice
func (p *Project) TargetFramework() string {
	for _, propGroup := range p.PropertyGroups {
		if propGroup.TargetFramework != "" {
			return propGroup.TargetFramework
		}
	}

	return ""
}

type PropertyGroup struct {
	OutputType      string `xml:"OutputType"`
	TargetFramework string `xml:"TargetFramework"`
	IsTestProject   string `xml:"IsTestProject"`
}

type ItemGroup struct {
	Compiles          []Compile          `xml:"Compile"`
	PackageReferences []PackageReference `xml:"PackageReference"`
	ProjectReferences []ProjectReference `xml:"ProjectReference"`
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

	return project, nil
}
