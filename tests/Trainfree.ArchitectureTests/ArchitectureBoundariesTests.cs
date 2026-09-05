using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Trainfree.ApiClients;
using Trainfree.Domain.Ids;
using Trainfree.Versioning;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Trainfree.ArchitectureTests;

public sealed class ArchitectureBoundariesTests
{
    // ArchUnitNET inspects IL type dependencies, so a ProjectReference nobody's code
    // actually uses yet (the exact moment the spec's "adds a project reference" scenario
    // describes) would compile clean and pass the rule below undetected. This check reads
    // the csproj files directly to close that gap. It matches the bare project name
    // rather than "Trainfree.Admin.csproj" specifically, so a hand-written assembly
    // <Reference>/<HintPath> pointing at Admin's DLL is caught too, not just a
    // ProjectReference to its csproj.
    [Theory]
    [InlineData("Trainfree.Domain")]
    [InlineData("Trainfree.Versioning")]
    [InlineData("Trainfree.ApiClients")]
    public void SharedLibraryProject_HasNoProjectReferenceToAdmin(string projectName)
    {
        // Arrange
        var csprojPath = Path.Combine(FindRepoRoot(), "src", projectName, $"{projectName}.csproj");
        var content = File.ReadAllText(csprojPath);

        // Act / Assert
        Assert.DoesNotContain("Trainfree.Admin", content, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (
            directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "Trainfree.slnx"))
        )
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                "Trainfree.slnx not found in any ancestor of the test output directory."
            );
    }

    // Trainfree.Admin exposes no public type for a shared library to reach through
    // InternalsVisibleTo -- loading it by assembly name keeps this test from needing one.
    private static readonly Architecture _architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(ProgramId).Assembly,
            typeof(VersionStamp).Assembly,
            typeof(ApiClientBase).Assembly,
            System.Reflection.Assembly.Load("Trainfree.Admin")
        )
        .Build();

    // Once Trainfree.Workout exists (roadmap slice 8, add-workout-runner-untimed or
    // earlier), add its assembly to the loader above and this namespace to the
    // forbidden-dependency list below.
    [Fact]
    public void SharedLibraries_DoNotDependOnAdmin()
    {
        // Arrange
        var rule = Types()
            .That()
            .ResideInNamespaceMatching(@"^Trainfree\.Domain(\..+)?$")
            .Or()
            .ResideInNamespaceMatching(@"^Trainfree\.Versioning(\..+)?$")
            .Or()
            .ResideInNamespaceMatching(@"^Trainfree\.ApiClients(\..+)?$")
            .Should()
            .NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^Trainfree\.Admin(\..+)?$"));

        // Act / Assert
        rule.Check(_architecture);
    }
}
