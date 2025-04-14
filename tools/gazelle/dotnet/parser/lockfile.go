package csproj

import (
	"encoding/json"
	"fmt"
	"os"
	"strings"
)

type PackagesLock struct {
	Version      int                              `json:"version"`
	Dependencies map[string]map[string]Dependency `json:"dependencies"`
}

type Dependency struct {
	Name     string
	Type     string `json:"type"`
	Resolved string `json:"resolved"`
	// contentHash is unfortunately unusable since it does not represent the
	// archive hash that would be downloaded by bazel.
}

// ParsePackagesLockFile parses the packages.lock.json file and returns a PackagesLock struct
func ParsePackagesLockFile(filePath string) (PackagesLock, error) {
	var packagesLock PackagesLock

	byteValue, err := os.ReadFile(filePath)
	if err != nil {
		return packagesLock, fmt.Errorf("error reading file: %v", err)
	}

	err = json.Unmarshal(byteValue, &packagesLock)
	if err != nil {
		return packagesLock, fmt.Errorf("error unmarshalling JSON: %v", err)
	}

	// Convert all keys in the Dependencies map to lowercase
	for framework, deps := range packagesLock.Dependencies {
		lowercaseFrameworkDeps := make(map[string]Dependency)
		for pkg, dep := range deps {
			dep.Name = pkg
			lowercaseFrameworkDeps[strings.ToLower(pkg)] = dep
		}
		packagesLock.Dependencies[framework] = lowercaseFrameworkDeps
	}

	return packagesLock, nil
}
