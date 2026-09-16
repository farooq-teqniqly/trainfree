using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Components;

namespace Trainfree.Admin.Tests.Components;

public sealed class AccessGateTests : BunitContext
{
    private static readonly RenderFragment ProtectedContent = builder =>
        builder.AddMarkupContent(0, """<p data-testid="protected-content">protected</p>""");

    private readonly IAccessCheck _accessCheck = Substitute.For<IAccessCheck>();

    public AccessGateTests() => Services.AddSingleton(_accessCheck);

    [Fact]
    public void Render_CheckStillPending_DoesNotRenderChildContent()
    {
        // Arrange
        var pendingCheck = new TaskCompletionSource<AccessCheckOutcome>();
        _accessCheck.CheckAsync(Arg.Any<CancellationToken>()).Returns(pendingCheck.Task);

        // Act
        var cut = Render<AccessGate>(p => p.Add(x => x.ChildContent, ProtectedContent));

        // Assert
        Assert.Empty(cut.FindAll("[data-testid=protected-content]"));
    }

    [Fact]
    public void Render_Administrator_RendersChildContent()
    {
        // Arrange
        _accessCheck.CheckAsync(Arg.Any<CancellationToken>()).Returns(new Administrator());

        // Act
        var cut = Render<AccessGate>(p => p.Add(x => x.ChildContent, ProtectedContent));

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid=protected-content]"));
    }

    [Fact]
    public void Render_NoAccess_RendersNoAccessPageInsteadOfChildContent()
    {
        // Arrange
        _accessCheck.CheckAsync(Arg.Any<CancellationToken>()).Returns(new NoAccess());

        // Act
        var cut = Render<AccessGate>(p => p.Add(x => x.ChildContent, ProtectedContent));

        // Assert
        Assert.Empty(cut.FindAll("[data-testid=protected-content]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=no-access-page]"));
    }

    [Fact]
    public void Render_AccessCheckFailed_RendersAccessCheckErrorPageInsteadOfChildContent()
    {
        // Arrange
        _accessCheck.CheckAsync(Arg.Any<CancellationToken>()).Returns(new AccessCheckFailed());

        // Act
        var cut = Render<AccessGate>(p => p.Add(x => x.ChildContent, ProtectedContent));

        // Assert
        Assert.Empty(cut.FindAll("[data-testid=protected-content]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=access-check-error-page]"));
    }

    [Fact]
    public void Render_ReauthenticationRequired_ForcesATopLevelReloadWithoutRenderingChildContent()
    {
        // Arrange
        _accessCheck
            .CheckAsync(Arg.Any<CancellationToken>())
            .Returns(new ReauthenticationRequired());

        // Act
        var cut = Render<AccessGate>(p => p.Add(x => x.ChildContent, ProtectedContent));

        // Assert
        Assert.Empty(cut.FindAll("[data-testid=protected-content]"));
        var navigation = Services.GetRequiredService<BunitNavigationManager>();
        var entry = Assert.Single(navigation.History);
        Assert.True(entry.Options.ForceLoad);
    }

    [Fact]
    public void Render_ReauthenticationRequired_DoesNotCallAccessCheckAgain()
    {
        // Arrange
        _accessCheck
            .CheckAsync(Arg.Any<CancellationToken>())
            .Returns(new ReauthenticationRequired());

        // Act
        Render<AccessGate>(p => p.Add(x => x.ChildContent, ProtectedContent));

        // Assert
        _accessCheck.Received(1).CheckAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispose_CheckStillInFlight_CancelsTheTokenPassedToCheckAsync()
    {
        // Arrange
        var pendingCheck = new TaskCompletionSource<AccessCheckOutcome>();
        var capturedToken = CancellationToken.None;
        _accessCheck
            .CheckAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedToken = callInfo.Arg<CancellationToken>();
                return pendingCheck.Task;
            });
        Render<AccessGate>(p => p.Add(x => x.ChildContent, ProtectedContent));

        // Act
        await DisposeComponentsAsync();

        // Assert
        Assert.True(capturedToken.IsCancellationRequested);
    }
}
