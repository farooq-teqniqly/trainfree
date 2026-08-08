using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Trainfree.Web.Layout;
using Trainfree.Web.Versioning;

namespace Trainfree.Web.Tests.Layout;

public sealed class MainLayoutTests : BunitContext
{
    private static readonly RenderFragment WorkingPage = builder =>
        builder.AddMarkupContent(0, """<p data-testid="page-body">page body</p>""");

    private static readonly RenderFragment FailingPage = builder =>
    {
        builder.OpenComponent<ThrowingComponent>(0);
        builder.CloseComponent();
    };

    private readonly IVersionCheck _versionCheck = Substitute.For<IVersionCheck>();

    public MainLayoutTests()
    {
        Services.AddSingleton(_versionCheck);
        Services.AddSingleton(new VersionStamp("v0.0.3", "e4f5g6h"));
    }

    [Fact]
    public void Render_VersionCheckThrows_KeepsRenderingThePageWithoutTheVersionIndicator()
    {
        // Arrange
        _versionCheck
            .CheckAsync(CancellationToken.None)
            .Returns<VersionCheckOutcome>(_ =>
                throw new InvalidTimeZoneException("arbitrary check failure")
            );

        // Act
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, WorkingPage));

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid=page-body]"));
        Assert.Empty(cut.FindAll(".version-stamp"));
        Assert.Empty(cut.FindAll(".blazor-error-boundary"));
    }

    [Fact]
    public void Render_PageThrows_ShowsThePageErrorAndKeepsTheRestOfTheLayout()
    {
        // Arrange
        _versionCheck.CheckAsync(CancellationToken.None).Returns(new VersionUnknown());

        // Act
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, FailingPage));

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid=page-error]"));
        Assert.NotEmpty(cut.FindAll(".version-stamp"));
        Assert.Empty(cut.FindAll(".blazor-error-boundary"));
    }

    [Fact]
    public void Render_PageAndVersionCheckBothSucceed_ShowsNeitherErrorUi()
    {
        // Arrange
        _versionCheck.CheckAsync(CancellationToken.None).Returns(new RunningLatestVersion());

        // Act
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, WorkingPage));

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid=page-body]"));
        Assert.NotEmpty(cut.FindAll(".version-stamp"));
        Assert.Empty(cut.FindAll("[data-testid=page-error]"));
    }
}
