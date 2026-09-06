using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Trainfree.Admin.Admin;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Admin;

public sealed class ExercisesApiClientTests : IDisposable
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;

    public ExercisesApiClientTests() =>
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://worker/api/") };

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task GetExercisesAsync_ServerReturns200_ReturnsMappedExercises()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                [
                    {"id":"EXR-AAAAAA","name":"Bodyweight Squat","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"},
                    {"id":"EXR-BBBBBB","name":"Skater Jump","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}
                ]
                """,
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var exercises = await client.GetExercisesAsync(CancellationToken.None);

        // Assert
        Assert.Collection(
            exercises,
            e =>
            {
                Assert.Equal(ExerciseId.Parse("EXR-AAAAAA"), e.Id);
                Assert.Equal("Bodyweight Squat", e.Name);
            },
            e =>
            {
                Assert.Equal(ExerciseId.Parse("EXR-BBBBBB"), e.Id);
                Assert.Equal("Skater Jump", e.Name);
            }
        );
    }

    [Fact]
    public async Task GetExercisesAsync_ServerReturnsEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var exercises = await client.GetExercisesAsync(CancellationToken.None);

        // Assert
        Assert.Empty(exercises);
    }

    [Fact]
    public async Task CreateExerciseAsync_ServerReturnsTheAccessLoginPage_ReturnsCreateExerciseFailed()
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
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.CreateExerciseAsync("New Exercise", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreateExerciseFailed>(outcome);
        Assert.Contains("302", failed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateExerciseAsync_ErrorBodyIsMalformedJson_ReturnsCreateExerciseFailed()
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
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.CreateExerciseAsync("New Exercise", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreateExerciseFailed>(outcome);
        Assert.Contains("502", failed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenameExerciseAsync_ServerReturns200_ReturnsRenameExerciseSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"EXR-AAAAAA","name":"Renamed","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.RenameExerciseAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            "Renamed",
            CancellationToken.None
        );

        // Assert
        var succeeded = Assert.IsType<RenameExerciseSucceeded>(outcome);
        Assert.Equal("Renamed", succeeded.Exercise.Name);
    }

    [Fact]
    public async Task RenameExerciseAsync_ServerReturns400_ReturnsRenameExerciseFailedWithServerError()
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
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.RenameExerciseAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            "Ab",
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<RenameExerciseFailed>(outcome);
        Assert.Equal("name must be between 4 and 100 characters", failed.Error);
    }

    [Fact]
    public async Task RenameExerciseAsync_ServerReturns404_ReturnsRenameExerciseFailed()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"exercise not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.RenameExerciseAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            "Renamed",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<RenameExerciseFailed>(outcome);
    }

    [Fact]
    public async Task CreateExerciseAsync_ServerReturns201_ReturnsCreateExerciseSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                """{"id":"EXR-AAAAAA","name":"New Exercise","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.CreateExerciseAsync("New Exercise", CancellationToken.None);

        // Assert
        var succeeded = Assert.IsType<CreateExerciseSucceeded>(outcome);
        Assert.Equal("New Exercise", succeeded.Exercise.Name);
    }

    [Fact]
    public async Task CreateExerciseAsync_ServerReturns409_ReturnsCreateExerciseFailedWithServerError()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                """{"error":"An exercise named \"New Exercise\" already exists."}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.CreateExerciseAsync("New Exercise", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreateExerciseFailed>(outcome);
        Assert.Equal("An exercise named \"New Exercise\" already exists.", failed.Error);
    }

    [Fact]
    public async Task DeleteExerciseAsync_ServerReturns204_ReturnsDeleteExerciseSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.DeleteExerciseAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteExerciseSucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteExerciseAsync_ServerReturns404_ReturnsDeleteExerciseSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"exercise not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.DeleteExerciseAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            CancellationToken.None
        );

        // Assert -- a 404 means the caller's desired end state already holds, so it is
        // treated as success rather than surfaced as an error.
        Assert.IsType<DeleteExerciseSucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteExerciseAsync_ServerReturns500_ReturnsDeleteExerciseFailedWithoutThrowing()
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
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.DeleteExerciseAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<DeleteExerciseFailed>(outcome);
        Assert.Equal("internal error", failed.Error);
    }

    [Fact]
    public async Task CreateExerciseAsync_NameIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.CreateExerciseAsync(null!, CancellationToken.None)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateExerciseAsync_NameIsEmptyOrWhiteSpace_ThrowsArgumentException(
        string name
    )
    {
        // Arrange
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.CreateExerciseAsync(name, CancellationToken.None)
        );
    }

    [Fact]
    public async Task RenameExerciseAsync_NameIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                null!,
                CancellationToken.None
            )
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenameExerciseAsync_NameIsEmptyOrWhiteSpace_ThrowsArgumentException(
        string name
    )
    {
        // Arrange
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.RenameExerciseAsync(ExerciseId.Parse("EXR-AAAAAA"), name, CancellationToken.None)
        );
    }

    [Fact]
    public async Task CreateExerciseAsync_HttpClientThrowsOperationCanceledException_ReturnsCreateExerciseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new OperationCanceledException("canceled");
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.CreateExerciseAsync("New Exercise", CancellationToken.None);

        // Assert
        Assert.IsType<CreateExerciseFailed>(outcome);
    }

    [Fact]
    public async Task RenameExerciseAsync_HttpClientThrowsOperationCanceledException_ReturnsRenameExerciseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new OperationCanceledException("canceled");
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.RenameExerciseAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            "Renamed",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<RenameExerciseFailed>(outcome);
    }

    [Fact]
    public async Task DeleteExerciseAsync_HttpClientThrowsOperationCanceledException_ReturnsDeleteExerciseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new OperationCanceledException("canceled");
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.DeleteExerciseAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteExerciseFailed>(outcome);
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
