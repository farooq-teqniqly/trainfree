using Trainfree.Admin.Components;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Components;

public sealed class ProgramRowTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_NameNullOrWhitespace_Throws(string? name)
    {
        // Arrange
        var id = ProgramId.Parse("PRG-AAAAAA");

        // Act / Assert
        Assert.ThrowsAny<ArgumentException>(() => new ProgramRow(id, name!));
    }

    [Fact]
    public void AddSession_SessionIsNull_Throws()
    {
        // Arrange
        var row = new ProgramRow(ProgramId.Parse("PRG-AAAAAA"), "Workout A");

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => row.AddSession(null!));
    }

    [Fact]
    public void RemoveSession_SessionIsNull_Throws()
    {
        // Arrange
        var row = new ProgramRow(ProgramId.Parse("PRG-AAAAAA"), "Workout A");

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => row.RemoveSession(null!));
    }
}
