using Trainfree.Admin.Components;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Components;

public sealed class SessionPhaseRowTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_DisplayNameNullOrWhitespace_Throws(string? displayName)
    {
        // Arrange
        var id = SessionPhaseId.Parse("SPH-AAAAAA");
        var phaseId = PhaseId.Parse("PHS-AAAAAA");

        // Act / Assert
        Assert.ThrowsAny<ArgumentException>(() => new SessionPhaseRow(id, phaseId, displayName!));
    }

    [Fact]
    public void AddProgramExercise_ProgramExerciseIsNull_Throws()
    {
        // Arrange
        var row = new SessionPhaseRow(
            SessionPhaseId.Parse("SPH-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA"),
            "Warm Up"
        );

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => row.AddProgramExercise(null!));
    }

    [Fact]
    public void AddProgramExercises_ProgramExercisesIsNull_Throws()
    {
        // Arrange
        var row = new SessionPhaseRow(
            SessionPhaseId.Parse("SPH-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA"),
            "Warm Up"
        );

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => row.AddProgramExercises(null!));
    }

    [Fact]
    public void RemoveProgramExercise_ProgramExerciseIsNull_Throws()
    {
        // Arrange
        var row = new SessionPhaseRow(
            SessionPhaseId.Parse("SPH-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA"),
            "Warm Up"
        );

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => row.RemoveProgramExercise(null!));
    }
}
