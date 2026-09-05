using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Trainfree.ApiClients.Tests;

public sealed class ApiClientBaseTests : IDisposable
{
    private readonly CapturingLoggerProvider _provider = new();
    private readonly ILoggerFactory _loggerFactory;

    public ApiClientBaseTests() =>
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_provider));

    public void Dispose()
    {
        _loggerFactory.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public async Task ReadErrorAsync_ResponseIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<ApiClientBaseTests>();

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TestApiClient.ReadErrorAsync(null!, logger, CancellationToken.None)
        );
    }

    [Fact]
    public async Task ReadErrorAsync_LoggerIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TestApiClient.ReadErrorAsync(response, null!, CancellationToken.None)
        );
    }

    [Fact]
    public async Task ReadErrorAsync_JsonErrorBody_ReturnsMessage()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"error":"name is required"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var logger = _loggerFactory.CreateLogger<ApiClientBaseTests>();

        // Act
        var message = await TestApiClient.ReadErrorAsync(response, logger, CancellationToken.None);

        // Assert
        Assert.Equal("name is required", message);
    }

    [Theory]
    [InlineData("non-json-content-type", "Request failed with status 302.")]
    [InlineData("malformed-json-body", "Request failed with status 502.")]
    public async Task ReadErrorAsync_UnreadableBody_ReturnsGenericFallbackWithoutThrowing(
        string scenario,
        string expectedMessage
    )
    {
        // Arrange
        using var response = CreateUnreadableResponse(scenario);
        var logger = _loggerFactory.CreateLogger<ApiClientBaseTests>();

        // Act
        var message = await TestApiClient.ReadErrorAsync(response, logger, CancellationToken.None);

        // Assert
        Assert.Equal(expectedMessage, message);
    }

    [Fact]
    public async Task ReadErrorAsync_JsonContentTypeWithUnparseableBody_LogsUnderCallingClientsCategory()
    {
        // Arrange
        using var response = CreateUnreadableResponse("malformed-json-body");
        var logger = _loggerFactory.CreateLogger<ApiClientBaseTests>();

        // Act
        await TestApiClient.ReadErrorAsync(response, logger, CancellationToken.None);

        // Assert
        var entry = Assert.Single(_provider.Entries);
        Assert.Equal(typeof(ApiClientBaseTests).FullName, entry.Category);
    }

    private static HttpResponseMessage CreateUnreadableResponse(string scenario) =>
        scenario switch
        {
            "non-json-content-type" => new HttpResponseMessage(HttpStatusCode.Found)
            {
                Content = new StringContent(
                    "<html><head><title>302 Found</title></head></html>",
                    Encoding.UTF8,
                    "text/html"
                ),
            },
            "malformed-json-body" => new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent(
                    "<html>gateway error</html>",
                    Encoding.UTF8,
                    "application/json"
                ),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

    private sealed class TestApiClient : ApiClientBase
    {
        public static new Task<string> ReadErrorAsync(
            HttpResponseMessage response,
            ILogger logger,
            CancellationToken cancellationToken
        ) => ApiClientBase.ReadErrorAsync(response, logger, cancellationToken);
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<(string Category, LogLevel Level)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) =>
            new CapturingLogger(categoryName, Entries);

        public void Dispose() { }

        private sealed class CapturingLogger : ILogger
        {
            private readonly string _category;
            private readonly List<(string Category, LogLevel Level)> _entries;

            public CapturingLogger(string category, List<(string Category, LogLevel Level)> entries)
            {
                _category = category;
                _entries = entries;
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            ) => _entries.Add((_category, logLevel));
        }
    }
}
