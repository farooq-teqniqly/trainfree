using Trainfree.Domain.Ids;
using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Domain.Tests.ProgramExercises;

public sealed class TimedProgramExerciseTests
{
    private static readonly ProgramExerciseId _id = ProgramExerciseId.Parse("PGX-7K2QXM");
    private static readonly SessionPhaseId _sessionPhaseId = SessionPhaseId.Parse("SPH-7K2QXM");
    private static readonly ExerciseId _exerciseId = ExerciseId.Parse("EXR-7K2QXM");

    private static TimedProgramExercise CreateValid(
        int durationSeconds = 30,
        decimal weight = 0,
        int sets = 3,
        int restSeconds = 60,
        ProgramExerciseSide side = ProgramExerciseSide.Both
    ) => new(_id, _sessionPhaseId, _exerciseId, durationSeconds, weight, sets, restSeconds, side);

    [Fact]
    public void Constructor_ValidValues_AssignsProperties()
    {
        // Act
        var result = CreateValid(
            durationSeconds: 30,
            weight: 45,
            sets: 3,
            restSeconds: 60,
            side: ProgramExerciseSide.Right
        );

        // Assert
        Assert.Equal(_id, result.Id);
        Assert.Equal(_sessionPhaseId, result.SessionPhaseId);
        Assert.Equal(_exerciseId, result.ExerciseId);
        Assert.Equal(30, result.DurationSeconds);
        Assert.Equal(45, result.Weight);
        Assert.Equal(3, result.Sets);
        Assert.Equal(60, result.RestSeconds);
        Assert.Equal(ProgramExerciseSide.Right, result.Side);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveDurationSeconds_ThrowsArgumentOutOfRangeException(
        int durationSeconds
    )
    {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateValid(durationSeconds: durationSeconds)
        );
    }

    [Fact]
    public void Constructor_NegativeWeight_ThrowsArgumentOutOfRangeException()
    {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateValid(weight: -1));
    }

    [Fact]
    public void Constructor_ZeroWeight_DoesNotThrow()
    {
        // Act
        var result = CreateValid(weight: 0);

        // Assert
        Assert.Equal(0, result.Weight);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveSets_ThrowsArgumentOutOfRangeException(int sets)
    {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateValid(sets: sets));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveRestSeconds_ThrowsArgumentOutOfRangeException(
        int restSeconds
    )
    {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateValid(restSeconds: restSeconds));
    }

    [Fact]
    public void Equals_SamePropertyValues_ReturnsTrue()
    {
        // Arrange
        var left = CreateValid();
        var right = CreateValid();

        // Act / Assert
        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentDurationSeconds_ReturnsFalse()
    {
        // Arrange
        var left = CreateValid(durationSeconds: 30);
        var right = CreateValid(durationSeconds: 45);

        // Act / Assert
        Assert.NotEqual(left, right);
        Assert.False(left == right);
        Assert.True(left != right);
    }
}
