namespace Trainfree.Versioning.Tests;

public sealed class VersionStampTests
{
    [Theory]
    [InlineData("v0.0.3+e4f5g6h", "v0.0.3", "e4f5g6h")]
    [InlineData("main+abc1234", "main", "abc1234")]
    [InlineData("v0.0.3+e4f5g6h.e594c70bec4c878dd9d65a4f08b37572b4d44992", "v0.0.3", "e4f5g6h")]
    public void FromInformationalVersion_StampedBuild_SplitsVersionFromCommit(
        string informationalVersion,
        string expectedVersion,
        string expectedCommit
    )
    {
        // Act
        var stamp = VersionStamp.FromInformationalVersion(informationalVersion);

        // Assert
        Assert.Equal(expectedVersion, stamp.Version);
        Assert.Equal(expectedCommit, stamp.Commit);
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("1.0.0+")]
    public void FromInformationalVersion_UnstampedLocalBuild_ReportsLocalCommit(
        string informationalVersion
    )
    {
        // Act
        var stamp = VersionStamp.FromInformationalVersion(informationalVersion);

        // Assert
        Assert.Equal("local", stamp.Commit);
    }

    [Fact]
    public void FromInformationalVersion_Null_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => VersionStamp.FromInformationalVersion(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromInformationalVersion_BlankValue_ThrowsArgumentException(
        string informationalVersion
    )
    {
        // Act / Assert
        Assert.Throws<ArgumentException>(() =>
            VersionStamp.FromInformationalVersion(informationalVersion)
        );
    }

    [Fact]
    public void Display_StampedBuild_ShowsVersionAndCommit()
    {
        // Arrange
        var stamp = new VersionStamp("v0.0.3", "e4f5g6h");

        // Act
        var display = stamp.Display;

        // Assert
        Assert.Equal("v0.0.3 (e4f5g6h)", display);
    }
}
