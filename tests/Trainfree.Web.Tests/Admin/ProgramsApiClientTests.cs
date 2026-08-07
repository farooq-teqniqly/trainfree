using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Trainfree.Web.Admin;
using Trainfree.Web.Ids;

namespace Trainfree.Web.Tests.Admin;

public sealed class ProgramsApiClientTests : IDisposable
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;

    public ProgramsApiClientTests() =>
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://worker/api/") };

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task CreateProgramAsync_ServerReturnsTheAccessLoginPage_ReturnsCreateProgramFailed()
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
        var client = new ProgramsApiClient(_httpClient, NullLogger<ProgramsApiClient>.Instance);

        // Act
        var outcome = await client.CreateProgramAsync("New Program", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreateProgramFailed>(outcome);
        Assert.Contains("302", failed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateProgramAsync_ErrorBodyIsMalformedJson_ReturnsCreateProgramFailed()
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
        var client = new ProgramsApiClient(_httpClient, NullLogger<ProgramsApiClient>.Instance);

        // Act
        var outcome = await client.CreateProgramAsync("New Program", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreateProgramFailed>(outcome);
        Assert.Contains("502", failed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenameProgramAsync_ServerReturns200_ReturnsRenameProgramSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"PRG-AAAAAA","name":"Renamed","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ProgramsApiClient(_httpClient, NullLogger<ProgramsApiClient>.Instance);

        // Act
        var outcome = await client.RenameProgramAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            "Renamed",
            CancellationToken.None
        );

        // Assert
        var succeeded = Assert.IsType<RenameProgramSucceeded>(outcome);
        Assert.Equal("Renamed", succeeded.Program.Name);
    }

    [Fact]
    public async Task RenameProgramAsync_ServerReturns400_ReturnsRenameProgramFailedWithServerError()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"error":"name must be between 5 and 100 characters"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ProgramsApiClient(_httpClient, NullLogger<ProgramsApiClient>.Instance);

        // Act
        var outcome = await client.RenameProgramAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            "Ab",
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<RenameProgramFailed>(outcome);
        Assert.Equal("name must be between 5 and 100 characters", failed.Error);
    }

    [Fact]
    public async Task RenameProgramAsync_ServerReturns404_ReturnsRenameProgramFailed()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"program not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ProgramsApiClient(_httpClient, NullLogger<ProgramsApiClient>.Instance);

        // Act
        var outcome = await client.RenameProgramAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            "Renamed",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<RenameProgramFailed>(outcome);
    }

    [Fact]
    public async Task CreateProgramAsync_ServerReturns201_ReturnsCreateProgramSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                """{"id":"PRG-AAAAAA","name":"New Program","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ProgramsApiClient(_httpClient, NullLogger<ProgramsApiClient>.Instance);

        // Act
        var outcome = await client.CreateProgramAsync("New Program", CancellationToken.None);

        // Assert
        var succeeded = Assert.IsType<CreateProgramSucceeded>(outcome);
        Assert.Equal("New Program", succeeded.Program.Name);
    }

    [Fact]
    public async Task CreateProgramAsync_ServerReturns409_ReturnsCreateProgramFailedWithServerError()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                """{"error":"A program named \"New Program\" already exists."}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ProgramsApiClient(_httpClient, NullLogger<ProgramsApiClient>.Instance);

        // Act
        var outcome = await client.CreateProgramAsync("New Program", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreateProgramFailed>(outcome);
        Assert.Equal("A program named \"New Program\" already exists.", failed.Error);
    }

    [Fact]
    public async Task DeleteProgramAsync_ServerReturns204_ReturnsDeleteProgramSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        var client = new ProgramsApiClient(_httpClient, NullLogger<ProgramsApiClient>.Instance);

        // Act
        var outcome = await client.DeleteProgramAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteProgramSucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteProgramAsync_ServerReturns404_ReturnsDeleteProgramSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"program not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ProgramsApiClient(_httpClient, NullLogger<ProgramsApiClient>.Instance);

        // Act
        var outcome = await client.DeleteProgramAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            CancellationToken.None
        );

        // Assert -- a 404 means the caller's desired end state already holds, so it is
        // treated as success rather than surfaced as an error.
        Assert.IsType<DeleteProgramSucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteProgramAsync_ServerReturns500_ReturnsDeleteProgramFailedWithoutThrowing()
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
        var client = new ProgramsApiClient(_httpClient, NullLogger<ProgramsApiClient>.Instance);

        // Act
        var outcome = await client.DeleteProgramAsync(
            ProgramId.Parse("PRG-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<DeleteProgramFailed>(outcome);
        Assert.Equal("internal error", failed.Error);
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage? NextResponse { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(NextResponse ?? new HttpResponseMessage(HttpStatusCode.OK));
    }
}
