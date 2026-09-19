using System.IO;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Trainfree.Admin.Admin;
using Trainfree.Domain.Users;
using Trainfree.Versioning;

namespace Trainfree.Admin.Tests;

public sealed class AppTests : BunitContext
{
    private static readonly CurrentUser TestUser = new(
        EmailAddress.Parse("farooq@example.com"),
        "Administrator"
    );

    private readonly IAccessCheck _accessCheck = Substitute.For<IAccessCheck>();
    private readonly IVersionCheck _versionCheck = Substitute.For<IVersionCheck>();
    private readonly IProgramsApiClient _programs = Substitute.For<IProgramsApiClient>();
    private readonly IProgramTreeApiClient _tree = Substitute.For<IProgramTreeApiClient>();
    private readonly ISessionsApiClient _sessions = Substitute.For<ISessionsApiClient>();
    private readonly IPhasesApiClient _phases = Substitute.For<IPhasesApiClient>();
    private readonly ISessionPhasesApiClient _sessionPhases =
        Substitute.For<ISessionPhasesApiClient>();
    private readonly IExercisesApiClient _exercises = Substitute.For<IExercisesApiClient>();
    private readonly IProgramExercisesApiClient _programExercises =
        Substitute.For<IProgramExercisesApiClient>();

    public AppTests()
    {
        Services.AddSingleton(_accessCheck);
        Services.AddSingleton(_versionCheck);
        Services.AddSingleton(_programs);
        Services.AddSingleton(_tree);
        Services.AddSingleton(_sessions);
        Services.AddSingleton(_phases);
        Services.AddSingleton(_sessionPhases);
        Services.AddSingleton(_exercises);
        Services.AddSingleton(_programExercises);
        Services.AddSingleton(new VersionStamp("v0.0.3", "e4f5g6h"));
        _versionCheck.CheckAsync(Arg.Any<CancellationToken>()).Returns(new RunningLatestVersion());
        _tree.GetProgramTreeAsync(Arg.Any<CancellationToken>()).Returns([]);
    }

    [Fact]
    public void Render_Administrator_RendersTheMatchedPageInsideMainLayout()
    {
        // Arrange
        _accessCheck.CheckAsync(Arg.Any<CancellationToken>()).Returns(new Administrator(TestUser));

        // Act
        var cut = Render<App>();

        // Assert
        Assert.NotEmpty(cut.FindAll(".navbar-brand"));
        Assert.Single(cut.FindAll(".version-stamp"));
        Assert.Equal("F", cut.Find("[data-testid=user-avatar]").TextContent.Trim());
    }

    [Fact]
    public void Render_NoAccess_DoesNotRenderTheRoutedPageOrMainLayout()
    {
        // Arrange
        _accessCheck.CheckAsync(Arg.Any<CancellationToken>()).Returns(new NoAccess());

        // Act
        var cut = Render<App>();

        // Assert
        Assert.Empty(cut.FindAll(".navbar-brand"));
        Assert.Empty(cut.FindAll(".version-stamp"));
        Assert.NotEmpty(cut.FindAll("[data-testid=no-access-page]"));
        _versionCheck.DidNotReceive().CheckAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Render_AccessCheckFailed_DoesNotRenderTheRoutedPageOrMainLayout()
    {
        // Arrange
        _accessCheck.CheckAsync(Arg.Any<CancellationToken>()).Returns(new AccessCheckFailed());

        // Act
        var cut = Render<App>();

        // Assert
        Assert.Empty(cut.FindAll(".navbar-brand"));
        Assert.Empty(cut.FindAll(".version-stamp"));
        Assert.NotEmpty(cut.FindAll("[data-testid=access-check-error-page]"));
        _versionCheck.DidNotReceive().CheckAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Render_AccessCheckThrowsAnUnexpectedException_ShowsAccessCheckErrorPageInsteadOfCrashing()
    {
        // Arrange -- an exception type IAccessCheck's contract doesn't document (e.g. a
        // connection dropped mid-body-read) is exactly the gap an ErrorBoundary around
        // AccessGate exists to contain.
        _accessCheck
            .CheckAsync(Arg.Any<CancellationToken>())
            .Returns<AccessCheckOutcome>(_ => throw new IOException("connection reset"));

        // Act
        var cut = Render<App>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid=access-check-error-page]"));
        Assert.Empty(cut.FindAll(".blazor-error-boundary"));
    }
}
