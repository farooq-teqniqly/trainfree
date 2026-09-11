using Bunit;
using Trainfree.Admin.Components;

namespace Trainfree.Admin.Tests.Components;

public sealed class InlineErrorTests : BunitContext
{
    [Fact]
    public void Render_MessageIsNull_RendersNothing()
    {
        // Arrange / Act
        var cut = Render<InlineError>(p => p.Add(c => c.TestId, "some-error"));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Render_MessageIsSet_RendersMessageWithTestId()
    {
        // Arrange / Act
        var cut = Render<InlineError>(p =>
            p.Add(c => c.Message, "Something went wrong.").Add(c => c.TestId, "some-error")
        );

        // Assert
        var element = cut.Find("[data-testid='some-error']");
        Assert.Equal("Something went wrong.", element.TextContent.Trim());
    }

    [Fact]
    public void Render_ClassIsSet_AppendsClassToRenderedElement()
    {
        // Arrange / Act
        var cut = Render<InlineError>(p =>
            p.Add(c => c.Message, "Something went wrong.")
                .Add(c => c.TestId, "some-error")
                .Add(c => c.Class, "mb-2")
        );

        // Assert
        var element = cut.Find("[data-testid='some-error']");
        Assert.Contains("mb-2", element.ClassList);
        Assert.Contains("text-danger", element.ClassList);
        Assert.Contains("small", element.ClassList);
    }
}
