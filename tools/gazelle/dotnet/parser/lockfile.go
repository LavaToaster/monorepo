package csproj

import (
	"encoding/json"
	"fmt"
	"os"
)

type PackagesLock struct {
	Version      int                              `json:"version"`
	Dependencies map[string]map[string]Dependency `json:"dependencies"`
}

type Dependency struct {
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

	return packagesLock, nil
}
