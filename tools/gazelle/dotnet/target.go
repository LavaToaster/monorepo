package dotnet

import (
	"github.com/emirpasic/gods/sets/treeset"

	parser "github.com/lavatoaster/dbot/tools/gazelle/dotnet/parser"
)

type DotnetPackageInfo struct {
	sources *treeset.Set
	project *parser.Project
	lock    *parser.PackagesLock
}

func newDotnetPackageInfo() *DotnetPackageInfo {
	return &DotnetPackageInfo{
		sources: treeset.NewWithStringComparator(),
		project: nil,
		lock:    nil,
	}
}
