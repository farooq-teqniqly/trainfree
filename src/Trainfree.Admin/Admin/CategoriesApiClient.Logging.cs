using Microsoft.Extensions.Logging;

namespace Trainfree.Admin.Admin;

internal sealed partial class CategoriesApiClient
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "A failure response from {RequestUri} declared {MediaType} but could not be "
            + "read; reporting status {StatusCode} instead."
    )]
    private static partial void LogErrorBodyUnreadable(
        ILogger logger,
        string? requestUri,
        string? mediaType,
        int statusCode,
        Exception exception
    );
}
