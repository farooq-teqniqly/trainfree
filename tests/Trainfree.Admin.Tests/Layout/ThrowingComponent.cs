using Microsoft.AspNetCore.Components;

namespace Trainfree.Admin.Tests.Layout;

/// <summary>A page stand-in whose initialization always fails.</summary>
public sealed class ThrowingComponent : ComponentBase
{
    protected override void OnInitialized() =>
        throw new InvalidTimeZoneException("arbitrary page failure");
}
