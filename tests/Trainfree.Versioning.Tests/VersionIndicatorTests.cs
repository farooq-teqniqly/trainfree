using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Trainfree.Versioning.Tests;

public sealed class VersionIndicatorTests : BunitContext
{
    private readonly IVersionCheck _versionCheck = Substitute.For<IVersionCheck>();

    public VersionIndicatorTests()
    {
        Services.AddSingleton(_versionCheck);
        Services.AddSingleton(new VersionStamp("v0.0.3", "e4f5g6h"));
    }

    [Fact]
    public void OnInitialized_RunningLatestVersion_ShowsTheBuildStampWithoutAnUpdateBanner()
    {
        // Arrange
        _versionCheck.CheckAsync(Arg.Any<CancellationToken>()).Returns(new RunningLatestVersion());

        // Act
        var cut = Render<VersionIndicator>();

        // Assert
        Assert.Contains("v0.0.3 (e4f5g6h)", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".version-update"));
    }

    [Fact]
    public void OnInitialized_RunningStaleVersion_ShowsAnUpdateBannerNamingTheDeployedVersion()
    {
        // Arrange
        _versionCheck
            .CheckAsync(Arg.Any<CancellationToken>())
            .Returns(new RunningStaleVersion(new VersionStamp("v0.0.4", "1234abc")));

        // Act
        var cut = Render<VersionIndicator>();

        // Assert
        var banner = cut.Find(".version-update");
        Assert.Contains("v0.0.4", banner.TextContent, StringComparison.Ordinal);
        Assert.NotNull(cut.Find(".version-update button"));
    }

    [Fact]
    public void OnInitialized_VersionUnknown_ShowsTheBuildStampWithoutAnUpdateBanner()
    {
        // Arrange
        _versionCheck.CheckAsync(Arg.Any<CancellationToken>()).Returns(new VersionUnknown());

        // Act
        var cut = Render<VersionIndicator>();

        // Assert
        Assert.Contains("v0.0.3 (e4f5g6h)", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".version-update"));
    }

    [Fact]
    public async Task Dispose_CheckStillInFlight_CancelsTheTokenPassedToCheckAsync()
    {
        // Arrange
        var pendingCheck = new TaskCompletionSource<VersionCheckOutcome>();
        var capturedToken = CancellationToken.None;
        _versionCheck
            .CheckAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedToken = callInfo.Arg<CancellationToken>();
                return pendingCheck.Task;
            });
        Render<VersionIndicator>();

        // Act
        await DisposeComponentsAsync();

        // Assert
        Assert.True(capturedToken.IsCancellationRequested);
    }
}
