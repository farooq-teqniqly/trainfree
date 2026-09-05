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
