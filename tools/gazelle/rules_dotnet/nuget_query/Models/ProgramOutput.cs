using System.Collections.Generic;

namespace nuget_query.Models;

public record ProgramOutput(List<string[]> DirectDependencies, List<BazelNugetPackage> BazelNugetPackages);