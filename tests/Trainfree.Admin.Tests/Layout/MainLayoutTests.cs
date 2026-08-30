using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Layout;
using Trainfree.Admin.Pages;
using Trainfree.Admin.Pages.Admin;
using Trainfree.Versioning;

namespace Trainfree.Admin.Tests.Layout;

public sealed class MainLayoutTests : BunitContext
{
    private static readonly RenderFragment WorkingPage = builder =>
        builder.AddMarkupContent(0, """<p data-testid="page-body">page body</p>""");

    private static readonly RenderFragment HomePage = builder =>
    {
        builder.OpenComponent<Home>(0);
        builder.CloseComponent();
    };

    private static readonly RenderFragment FailingPage = builder =>
    {
        builder.OpenComponent<ThrowingComponent>(0);
        builder.CloseComponent();
    };

    // A real routed page, so the write-path containment is proven where it actually failed
    // rather than through a stand-in that only approximates an event handler.
    private static readonly RenderFragment AdminPage = builder =>
    {
        builder.OpenComponent<Programs>(0);
        builder.CloseComponent();
    };

    private readonly IVersionCheck _versionCheck = Substitute.For<IVersionCheck>();
    private readonly IProgramsApiClient _programs = Substitute.For<IProgramsApiClient>();
    private readonly ISessionsApiClient _sessions = Substitute.For<ISessionsApiClient>();

    public MainLayoutTests()
    {
        Services.AddSingleton(_versionCheck);
        Services.AddSingleton(_programs);
        Services.AddSingleton(_sessions);
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
    public void Render_PageWriteFails_ShowsThePageErrorInsteadOfBlankingTheApp()
    {
        // Arrange
        _versionCheck.CheckAsync(CancellationToken.None).Returns(new VersionUnknown());
        _programs.GetProgramsAsync(CancellationToken.None).Returns([]);
        _programs
            .CreateProgramAsync("New Program", CancellationToken.None)
            .Returns<CreateProgramOutcome>(_ => throw new HttpRequestException("network down"));
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, AdminPage));

        // Act
        cut.Find("[data-testid=add-program]").Click();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid=page-error]"));
        Assert.NotEmpty(cut.FindAll(".version-stamp"));
    }

    [Fact]
    public void Render_NavigatingAfterAPageFailure_ClearsThePageError()
    {
        // Arrange
        _versionCheck.CheckAsync(CancellationToken.None).Returns(new VersionUnknown());
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, FailingPage));

        // Act
        cut.Render(p => p.Add(x => x.Body, WorkingPage));

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid=page-body]"));
        Assert.Empty(cut.FindAll("[data-testid=page-error]"));
    }

    [Fact]
    public void Render_NavigatingAfterAVersionCheckFailure_BringsTheIndicatorBack()
    {
        // Arrange
        _versionCheck
            .CheckAsync(CancellationToken.None)
            .Returns<VersionCheckOutcome>(
                _ => throw new InvalidTimeZoneException("arbitrary check failure"),
                _ => new RunningLatestVersion()
            );
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, WorkingPage));

        // Act
        cut.Render(p => p.Add(x => x.Body, WorkingPage));

        // Assert
        Assert.NotEmpty(cut.FindAll(".version-stamp"));
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

    [Fact]
    public void Render_Always_ShowsTheTrainfreeAdminBrand()
    {
        // Arrange
        _versionCheck.CheckAsync(CancellationToken.None).Returns(new RunningLatestVersion());

        // Act
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, WorkingPage));

        // Assert
        var brand = cut.Find(".navbar-brand");
        Assert.Contains("Trainfree Admin", brand.TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(brand.QuerySelectorAll("svg"));
    }

    [Fact]
    public void Render_HomePage_ShowsTheVersionIndicatorExactlyOnce()
    {
        // Arrange
        _versionCheck.CheckAsync(CancellationToken.None).Returns(new RunningLatestVersion());

        // Act
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, HomePage));

        // Assert
        Assert.Single(cut.FindAll(".version-stamp"));
    }

    [Fact]
    public void Render_ClickingTheNavToggler_ExpandsTheCollapsedSidebar()
    {
        // Arrange
        _versionCheck.CheckAsync(CancellationToken.None).Returns(new RunningLatestVersion());
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, WorkingPage));
        Assert.Contains("collapse", cut.Find("nav.sidebar").ClassList);

        // Act
        cut.Find(".navbar-toggler").Click();

        // Assert
        Assert.DoesNotContain("collapse", cut.Find("nav.sidebar").ClassList);
    }
}
