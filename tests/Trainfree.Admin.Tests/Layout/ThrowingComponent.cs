using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Trainfree.Admin.Tests.Layout;

/// <summary>A page stand-in whose initialization always fails.</summary>
[SuppressMessage(
    "Performance",
    "CA1812",
    Justification = "Instantiated by RenderTreeBuilder.OpenComponent<T> as a generic type argument in MainLayoutTests, which the analyzer cannot see."
)]
internal sealed class ThrowingComponent : ComponentBase
{
    protected override void OnInitialized() =>
        throw new InvalidTimeZoneException("arbitrary page failure");
}
