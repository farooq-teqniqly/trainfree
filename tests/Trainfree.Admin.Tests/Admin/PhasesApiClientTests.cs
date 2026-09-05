using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Trainfree.Admin.Admin;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Admin;

public sealed class PhasesApiClientTests : IDisposable
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;

    public PhasesApiClientTests() =>
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://worker/api/") };

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task GetPhasesAsync_ServerReturns200_ReturnsMappedPhases()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                [
                    {"id":"PHS-AAAAAA","name":"Warm Up","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"},
                    {"id":"PHS-BBBBBB","name":"Cool Down","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}
                ]
                """,
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var phases = await client.GetPhasesAsync(CancellationToken.None);

        // Assert
        Assert.Collection(
            phases,
            p =>
            {
                Assert.Equal(PhaseId.Parse("PHS-AAAAAA"), p.Id);
                Assert.Equal("Warm Up", p.Name);
            },
            p =>
            {
                Assert.Equal(PhaseId.Parse("PHS-BBBBBB"), p.Id);
                Assert.Equal("Cool Down", p.Name);
            }
        );
    }

    [Fact]
    public async Task GetPhasesAsync_ServerReturnsEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        };
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var phases = await client.GetPhasesAsync(CancellationToken.None);

        // Assert
        Assert.Empty(phases);
    }

    [Fact]
    public async Task CreatePhaseAsync_ServerReturnsTheAccessLoginPage_ReturnsCreatePhaseFailed()
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
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.CreatePhaseAsync("New Phase", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreatePhaseFailed>(outcome);
        Assert.Contains("302", failed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreatePhaseAsync_ErrorBodyIsMalformedJson_ReturnsCreatePhaseFailed()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(
                "<html>gateway error</html>",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.CreatePhaseAsync("New Phase", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreatePhaseFailed>(outcome);
        Assert.Contains("502", failed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenamePhaseAsync_ServerReturns200_ReturnsRenamePhaseSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"PHS-AAAAAA","name":"Renamed","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.RenamePhaseAsync(
            PhaseId.Parse("PHS-AAAAAA"),
            "Renamed",
            CancellationToken.None
        );

        // Assert
        var succeeded = Assert.IsType<RenamePhaseSucceeded>(outcome);
        Assert.Equal("Renamed", succeeded.Phase.Name);
    }

    [Fact]
    public async Task RenamePhaseAsync_ServerReturns400_ReturnsRenamePhaseFailedWithServerError()
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
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.RenamePhaseAsync(
            PhaseId.Parse("PHS-AAAAAA"),
            "Ab",
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<RenamePhaseFailed>(outcome);
        Assert.Equal("name must be between 4 and 100 characters", failed.Error);
    }

    [Fact]
    public async Task RenamePhaseAsync_ServerReturns404_ReturnsRenamePhaseFailed()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"phase not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.RenamePhaseAsync(
            PhaseId.Parse("PHS-AAAAAA"),
            "Renamed",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<RenamePhaseFailed>(outcome);
    }

    [Fact]
    public async Task CreatePhaseAsync_ServerReturns201_ReturnsCreatePhaseSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                """{"id":"PHS-AAAAAA","name":"New Phase","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.CreatePhaseAsync("New Phase", CancellationToken.None);

        // Assert
        var succeeded = Assert.IsType<CreatePhaseSucceeded>(outcome);
        Assert.Equal("New Phase", succeeded.Phase.Name);
    }

    [Fact]
    public async Task CreatePhaseAsync_ServerReturns409_ReturnsCreatePhaseFailedWithServerError()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                """{"error":"A phase named \"New Phase\" already exists."}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.CreatePhaseAsync("New Phase", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreatePhaseFailed>(outcome);
        Assert.Equal("A phase named \"New Phase\" already exists.", failed.Error);
    }

    [Fact]
    public async Task DeletePhaseAsync_ServerReturns204_ReturnsDeletePhaseSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.DeletePhaseAsync(
            PhaseId.Parse("PHS-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeletePhaseSucceeded>(outcome);
    }

    [Fact]
    public async Task DeletePhaseAsync_ServerReturns404_ReturnsDeletePhaseSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"phase not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.DeletePhaseAsync(
            PhaseId.Parse("PHS-AAAAAA"),
            CancellationToken.None
        );

        // Assert -- a 404 means the caller's desired end state already holds, so it is
        // treated as success rather than surfaced as an error.
        Assert.IsType<DeletePhaseSucceeded>(outcome);
    }

    [Fact]
    public async Task DeletePhaseAsync_ServerReturns500_ReturnsDeletePhaseFailedWithoutThrowing()
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
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.DeletePhaseAsync(
            PhaseId.Parse("PHS-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<DeletePhaseFailed>(outcome);
        Assert.Equal("internal error", failed.Error);
    }

    [Fact]
    public async Task CreatePhaseAsync_NameIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.CreatePhaseAsync(null!, CancellationToken.None)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePhaseAsync_NameIsEmptyOrWhiteSpace_ThrowsArgumentException(string name)
    {
        // Arrange
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.CreatePhaseAsync(name, CancellationToken.None)
        );
    }

    [Fact]
    public async Task RenamePhaseAsync_NameIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.RenamePhaseAsync(PhaseId.Parse("PHS-AAAAAA"), null!, CancellationToken.None)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenamePhaseAsync_NameIsEmptyOrWhiteSpace_ThrowsArgumentException(string name)
    {
        // Arrange
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.RenamePhaseAsync(PhaseId.Parse("PHS-AAAAAA"), name, CancellationToken.None)
        );
    }

    [Fact]
    public async Task CreatePhaseAsync_HttpClientThrowsOperationCanceledException_ReturnsCreatePhaseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new OperationCanceledException("canceled");
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.CreatePhaseAsync("New Phase", CancellationToken.None);

        // Assert
        Assert.IsType<CreatePhaseFailed>(outcome);
    }

    [Fact]
    public async Task RenamePhaseAsync_HttpClientThrowsOperationCanceledException_ReturnsRenamePhaseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new OperationCanceledException("canceled");
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.RenamePhaseAsync(
            PhaseId.Parse("PHS-AAAAAA"),
            "Renamed",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<RenamePhaseFailed>(outcome);
    }

    [Fact]
    public async Task DeletePhaseAsync_HttpClientThrowsOperationCanceledException_ReturnsDeletePhaseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new OperationCanceledException("canceled");
        var client = new PhasesApiClient(_httpClient, NullLogger<PhasesApiClient>.Instance);

        // Act
        var outcome = await client.DeletePhaseAsync(
            PhaseId.Parse("PHS-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeletePhaseFailed>(outcome);
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
