using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Trainfree.Admin.Admin;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Admin;

public sealed class SessionPhasesApiClientTests : IDisposable
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;

    public SessionPhasesApiClientTests() =>
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://worker/api/") };

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task GetSessionPhasesAsync_ServerReturnsSessionPhases_ReturnsMappedSummaries()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                [
                    {"id":"SPH-AAAAAA","sessionId":"SNN-AAAAAA","phaseId":"PHS-AAAAAA","createdAt":"2026-01-01T00:00:00.000Z"},
                    {"id":"SPH-BBBBBB","sessionId":"SNN-AAAAAA","phaseId":"PHS-BBBBBB","createdAt":"2026-01-02T00:00:00.000Z"}
                ]
                """,
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionPhasesApiClient(
            _httpClient,
            NullLogger<SessionPhasesApiClient>.Instance
        );

        // Act
        var sessionPhases = await client.GetSessionPhasesAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.Collection(
            sessionPhases,
            sp =>
            {
                Assert.Equal(SessionPhaseId.Parse("SPH-AAAAAA"), sp.Id);
                Assert.Equal(SessionId.Parse("SNN-AAAAAA"), sp.SessionId);
                Assert.Equal(PhaseId.Parse("PHS-AAAAAA"), sp.PhaseId);
            },
            sp =>
            {
                Assert.Equal(SessionPhaseId.Parse("SPH-BBBBBB"), sp.Id);
                Assert.Equal(PhaseId.Parse("PHS-BBBBBB"), sp.PhaseId);
            }
        );
    }

    [Fact]
    public async Task GetSessionPhasesAsync_ServerReturnsEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        };
        var client = new SessionPhasesApiClient(
            _httpClient,
            NullLogger<SessionPhasesApiClient>.Instance
        );

        // Act
        var sessionPhases = await client.GetSessionPhasesAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.Empty(sessionPhases);
    }

    [Fact]
    public async Task CreateSessionPhaseAsync_ServerReturns201_ReturnsCreateSessionPhaseSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                """{"id":"SPH-AAAAAA","sessionId":"SNN-AAAAAA","phaseId":"PHS-AAAAAA","createdAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionPhasesApiClient(
            _httpClient,
            NullLogger<SessionPhasesApiClient>.Instance
        );

        // Act
        var outcome = await client.CreateSessionPhaseAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        var succeeded = Assert.IsType<CreateSessionPhaseSucceeded>(outcome);
        Assert.Equal(PhaseId.Parse("PHS-AAAAAA"), succeeded.SessionPhase.PhaseId);
        Assert.Equal(SessionId.Parse("SNN-AAAAAA"), succeeded.SessionPhase.SessionId);
    }

    [Fact]
    public async Task CreateSessionPhaseAsync_ServerReturns400_ReturnsCreateSessionPhaseFailedWithServerError()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"error":"phaseId is required and must reference an existing phase"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionPhasesApiClient(
            _httpClient,
            NullLogger<SessionPhasesApiClient>.Instance
        );

        // Act
        var outcome = await client.CreateSessionPhaseAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<CreateSessionPhaseFailed>(outcome);
        Assert.Equal("phaseId is required and must reference an existing phase", failed.Error);
    }

    [Fact]
    public async Task CreateSessionPhaseAsync_ServerReturns404_ReturnsCreateSessionPhaseFailed()
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
        var client = new SessionPhasesApiClient(
            _httpClient,
            NullLogger<SessionPhasesApiClient>.Instance
        );

        // Act
        var outcome = await client.CreateSessionPhaseAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<CreateSessionPhaseFailed>(outcome);
    }

    [Fact]
    public async Task CreateSessionPhaseAsync_HttpClientThrowsJsonException_ReturnsCreateSessionPhaseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new JsonException("malformed body");
        var client = new SessionPhasesApiClient(
            _httpClient,
            NullLogger<SessionPhasesApiClient>.Instance
        );

        // Act
        var outcome = await client.CreateSessionPhaseAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<CreateSessionPhaseFailed>(outcome);
    }

    [Fact]
    public async Task DeleteSessionPhaseAsync_ServerReturns204_ReturnsDeleteSessionPhaseSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        var client = new SessionPhasesApiClient(
            _httpClient,
            NullLogger<SessionPhasesApiClient>.Instance
        );

        // Act
        var outcome = await client.DeleteSessionPhaseAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            SessionPhaseId.Parse("SPH-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteSessionPhaseSucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteSessionPhaseAsync_ServerReturns404_ReturnsDeleteSessionPhaseSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"session phase not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new SessionPhasesApiClient(
            _httpClient,
            NullLogger<SessionPhasesApiClient>.Instance
        );

        // Act
        var outcome = await client.DeleteSessionPhaseAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            SessionPhaseId.Parse("SPH-AAAAAA"),
            CancellationToken.None
        );

        // Assert -- a 404 means the caller's desired end state already holds, so it is
        // treated as success rather than surfaced as an error.
        Assert.IsType<DeleteSessionPhaseSucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteSessionPhaseAsync_ServerReturns500_ReturnsDeleteSessionPhaseFailedWithoutThrowing()
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
        var client = new SessionPhasesApiClient(
            _httpClient,
            NullLogger<SessionPhasesApiClient>.Instance
        );

        // Act
        var outcome = await client.DeleteSessionPhaseAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            SessionPhaseId.Parse("SPH-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<DeleteSessionPhaseFailed>(outcome);
        Assert.Equal("internal error", failed.Error);
    }

    [Fact]
    public async Task DeleteSessionPhaseAsync_HttpClientThrowsJsonException_ReturnsDeleteSessionPhaseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new JsonException("malformed body");
        var client = new SessionPhasesApiClient(
            _httpClient,
            NullLogger<SessionPhasesApiClient>.Instance
        );

        // Act
        var outcome = await client.DeleteSessionPhaseAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            SessionPhaseId.Parse("SPH-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteSessionPhaseFailed>(outcome);
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
