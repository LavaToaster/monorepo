package rules_dotnet

import (
	"encoding/json"
	"fmt"
	"os"
	"os/exec"
)

type ProgramOutput struct {
	DirectDependencies [][]string          `json:"DirectDependencies"`
	BazelNugetPackages []BazelNugetPackage `json:"BazelNugetPackages"`
}

type QueryResult struct {
	Result *ProgramOutput
	Error  error
}

func DiscoverNuGetPackages(nugetQueryDir string, lockFile string) QueryResult {
	// Validate inputs
	if len(lockFile) == 0 {
		return QueryResult{nil, fmt.Errorf("no lock file provided")}
	}

	// Prepare the command arguments
	args := []string{"run"}
	args = append(args, "--lock-file", lockFile)

	// Create and execute the command
	// Get the directory of the current source file
	_, err := os.Executable()
	if err != nil {
		return QueryResult{nil, fmt.Errorf("error determining executable path: %w", err)}
	}

	// Create the command
	cmd := exec.Command("dotnet", args...)
	cmd.Dir = nugetQueryDir
	cmd.Stderr = os.Stderr

	// Run the command and capture output
	output, err := cmd.Output()
	if err != nil {
		return QueryResult{nil, fmt.Errorf("error executing dotnet command: %w", err)}
	}

	// Parse the output as JSON
	var result ProgramOutput
	if err := json.Unmarshal(output, &result); err != nil {
		return QueryResult{nil, fmt.Errorf("error parsing JSON output: %w", err)}
	}

	return QueryResult{Result: &result, Error: nil}
}
