using Microsoft.Extensions.Logging;

namespace Trainfree.Admin.Pages;

public partial class Categories
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load categories from the API.")]
    private static partial void LogLoadCategoriesFailed(ILogger logger, Exception exception);
}
