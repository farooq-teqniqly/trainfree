using Trainfree.Admin.Components;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Components;

public sealed class SessionRowTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_NameNullOrWhitespace_Throws(string? name)
    {
        // Arrange
        var id = SessionId.Parse("SNN-AAAAAA");
        var programId = ProgramId.Parse("PRG-AAAAAA");

        // Act / Assert
        Assert.ThrowsAny<ArgumentException>(() => new SessionRow(id, programId, name!));
    }

    [Fact]
    public void AddPhase_PhaseIsNull_Throws()
    {
        // Arrange
        var row = new SessionRow(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Session A"
        );

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => row.AddPhase(null!));
    }

    [Fact]
    public void RemovePhase_PhaseIsNull_Throws()
    {
        // Arrange
        var row = new SessionRow(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Session A"
        );

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => row.RemovePhase(null!));
    }
}
