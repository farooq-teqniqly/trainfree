using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Trainfree.Admin.Admin;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Admin;

public sealed class SessionsApiClientTests : IDisposable
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;

    public SessionsApiClientTests() =>
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://worker/api/") };

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task GetSessionsAsync_ServerReturnsSessions_ReturnsMappedSessionSummaries()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                [
                    {"id":"SNN-AAAAAA","programId":"PRG-AAAAAA","name":"Monday Lower Body","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"},
                    {"id":"SNN-BBBBBB","programId":"PRG-AAAAAA","name":"Wednesday Upper Body","createdAt":"2026-01-02T00:00:00.000Z","updatedAt":"2026-01-02T00:00:00.000Z"}
                ]
                """,
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var sessions = await client.GetSessionsAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.Collection(
            sessions,
            session =>
            {
                Assert.Equal(SessionId.Parse("SNN-AAAAAA"), session.Id);
                Assert.Equal(ProgramId.Parse("PRG-AAAAAA"), session.ProgramId);
                Assert.Equal("Monday Lower Body", session.Name);
            },
            session =>
            {
                Assert.Equal(SessionId.Parse("SNN-BBBBBB"), session.Id);
                Assert.Equal(ProgramId.Parse("PRG-AAAAAA"), session.ProgramId);
                Assert.Equal("Wednesday Upper Body", session.Name);
            }
        );
    }

    [Fact]
    public async Task GetSessionsAsync_ServerReturnsEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        };
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var sessions = await client.GetSessionsAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task CreateSessionAsync_ServerReturns201_ReturnsCreateSessionSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                """{"id":"SNN-AAAAAA","programId":"PRG-AAAAAA","name":"Monday Lower Body","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.CreateSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body",
            CancellationToken.None
        );

        // Assert
        var succeeded = Assert.IsType<CreateSessionSucceeded>(outcome);
        Assert.Equal("Monday Lower Body", succeeded.Session.Name);
        Assert.Equal(ProgramId.Parse("PRG-AAAAAA"), succeeded.Session.ProgramId);
    }

    [Fact]
    public async Task CreateSessionAsync_ServerReturns409_ReturnsCreateSessionFailedWithServerError()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                """{"error":"A session named \"Monday Lower Body\" already exists."}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.CreateSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body",
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<CreateSessionFailed>(outcome);
        Assert.Equal("A session named \"Monday Lower Body\" already exists.", failed.Error);
    }

    [Fact]
    public async Task CreateSessionAsync_ServerReturnsTheAccessLoginPage_ReturnsCreateSessionFailed()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Found)
        {
            Content = new StringContent(
                "<html><head><title>302 Found</title></head></html>",
                Encoding.UTF8,
                "text/html"
            ),
        };
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.CreateSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body",
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<CreateSessionFailed>(outcome);
        Assert.Contains("302", failed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenameSessionAsync_ServerReturns200_ReturnsRenameSessionSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"SNN-AAAAAA","programId":"PRG-AAAAAA","name":"Renamed Session","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.RenameSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            "Renamed Session",
            CancellationToken.None
        );

        // Assert
        var succeeded = Assert.IsType<RenameSessionSucceeded>(outcome);
        Assert.Equal("Renamed Session", succeeded.Session.Name);
    }

    [Fact]
    public async Task RenameSessionAsync_ServerReturns400_ReturnsRenameSessionFailedWithServerError()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"error":"name must be between 4 and 100 characters"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.RenameSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            "Ab",
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<RenameSessionFailed>(outcome);
        Assert.Equal("name must be between 4 and 100 characters", failed.Error);
    }

    [Fact]
    public async Task RenameSessionAsync_ServerReturns404_ReturnsRenameSessionFailed()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"session not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.RenameSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            "Renamed Session",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<RenameSessionFailed>(outcome);
    }

    [Fact]
    public async Task DeleteSessionAsync_ServerReturns204_ReturnsDeleteSessionSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.DeleteSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteSessionSucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteSessionAsync_ServerReturns404_ReturnsDeleteSessionSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"session not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.DeleteSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            CancellationToken.None
        );

        // Assert -- a 404 means the caller's desired end state already holds, so it is
        // treated as success rather than surfaced as an error.
        Assert.IsType<DeleteSessionSucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteSessionAsync_ServerReturns500_ReturnsDeleteSessionFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(
                """{"error":"internal error"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.DeleteSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<DeleteSessionFailed>(outcome);
        Assert.Equal("internal error", failed.Error);
    }

    [Fact]
    public async Task CreateSessionAsync_NameIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.CreateSessionAsync(ProgramId.Parse("PRG-AAAAAA"), null!, CancellationToken.None)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateSessionAsync_NameIsEmptyOrWhiteSpace_ThrowsArgumentException(
        string name
    )
    {
        // Arrange
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.CreateSessionAsync(ProgramId.Parse("PRG-AAAAAA"), name, CancellationToken.None)
        );
    }

    [Fact]
    public async Task RenameSessionAsync_NameIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.RenameSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                null!,
                CancellationToken.None
            )
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenameSessionAsync_NameIsEmptyOrWhiteSpace_ThrowsArgumentException(
        string name
    )
    {
        // Arrange
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.RenameSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                name,
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task CreateSessionAsync_HttpClientThrowsJsonException_ReturnsCreateSessionFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new JsonException("malformed body");
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.CreateSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<CreateSessionFailed>(outcome);
    }

    [Fact]
    public async Task RenameSessionAsync_HttpClientThrowsJsonException_ReturnsRenameSessionFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new JsonException("malformed body");
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.RenameSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            "Renamed Session",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<RenameSessionFailed>(outcome);
    }

    [Fact]
    public async Task DeleteSessionAsync_HttpClientThrowsJsonException_ReturnsDeleteSessionFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new JsonException("malformed body");
        var client = new SessionsApiClient(_httpClient, NullLogger<SessionsApiClient>.Instance);

        // Act
        var outcome = await client.DeleteSessionAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteSessionFailed>(outcome);
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage? NextResponse { get; set; }
        public Exception? NextException { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            NextException is not null
                ? Task.FromException<HttpResponseMessage>(NextException)
                : Task.FromResult(NextResponse ?? new HttpResponseMessage(HttpStatusCode.OK));
    }
}
