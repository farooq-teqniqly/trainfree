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
                    {"id":"EXR-AAAAAA","name":"Bodyweight Squat","imageUrl":"/api/exercises/EXR-AAAAAA/image?v=abc","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"},
                    {"id":"EXR-BBBBBB","name":"Skater Jump","imageUrl":null,"createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}
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
                Assert.Equal("/api/exercises/EXR-AAAAAA/image?v=abc", e.ImageUrl);
            },
            e =>
            {
                Assert.Equal(ExerciseId.Parse("EXR-BBBBBB"), e.Id);
                Assert.Equal("Skater Jump", e.Name);
                Assert.Null(e.ImageUrl);
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
    public async Task CreateExerciseAsync_ServerReturnsNullBody_ReturnsCreateExerciseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json"),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.CreateExerciseAsync("New Exercise", CancellationToken.None);

        // Assert
        Assert.IsType<CreateExerciseFailed>(outcome);
    }

    [Fact]
    public async Task CreateExerciseAsync_ServerReturnsMalformedId_ReturnsCreateExerciseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                """{"id":"not-a-valid-id","name":"New Exercise","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.CreateExerciseAsync("New Exercise", CancellationToken.None);

        // Assert
        Assert.IsType<CreateExerciseFailed>(outcome);
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
    public async Task RenameExerciseAsync_ServerReturnsNullBody_ReturnsRenameExerciseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json"),
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
    public async Task RenameExerciseAsync_ServerReturnsMalformedId_ReturnsRenameExerciseFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"not-a-valid-id","name":"Renamed","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
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

    [Fact]
    public async Task UploadExerciseImageAsync_ServerReturns200_ReturnsUploadExerciseImageSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"EXR-AAAAAA","name":"Bodyweight Squat","imageUrl":"/api/exercises/EXR-AAAAAA/image?v=new","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.UploadExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            new StagedImage([1, 2, 3], "image/png"),
            CancellationToken.None
        );

        // Assert
        var succeeded = Assert.IsType<UploadExerciseImageSucceeded>(outcome);
        Assert.Equal(ExerciseId.Parse("EXR-AAAAAA"), succeeded.Exercise.Id);
        Assert.Equal("/api/exercises/EXR-AAAAAA/image?v=new", succeeded.Exercise.ImageUrl);
    }

    [Fact]
    public async Task UploadExerciseImageAsync_StagedImage_SendsRawBytesWithContentTypeToImageEndpoint()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"EXR-AAAAAA","name":"Bodyweight Squat","imageUrl":"/api/exercises/EXR-AAAAAA/image?v=new","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        await client.UploadExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            new StagedImage([1, 2, 3], "image/jpeg"),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpMethod.Put, _handler.LastMethod);
        Assert.Equal("/api/exercises/EXR-AAAAAA/image", _handler.LastRequestUri?.AbsolutePath);
        Assert.Equal("image/jpeg", _handler.LastContentType);
        Assert.Equal(new byte[] { 1, 2, 3 }, _handler.LastBody);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, "image must be at most 1048576 bytes")]
    [InlineData(HttpStatusCode.UnsupportedMediaType, "only JPEG and PNG images are supported")]
    [InlineData(HttpStatusCode.NotFound, "exercise not found")]
    [InlineData(HttpStatusCode.BadRequest, "image body must not be empty")]
    public async Task UploadExerciseImageAsync_ServerReturnsError_ReturnsUploadExerciseImageFailedWithServerError(
        HttpStatusCode status,
        string error
    )
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(status)
        {
            Content = new StringContent(
                $$"""{"error":"{{error}}"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.UploadExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            new StagedImage([1, 2, 3], "image/png"),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<UploadExerciseImageFailed>(outcome);
        Assert.Equal(error, failed.Error);
    }

    [Fact]
    public async Task UploadExerciseImageAsync_ServerReturns403WithEmptyBody_ReturnsUploadExerciseImageFailedWithStatusFallback()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Forbidden);
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.UploadExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            new StagedImage([1, 2, 3], "image/png"),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<UploadExerciseImageFailed>(outcome);
        Assert.Contains("403", failed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadExerciseImageAsync_ServerReturnsNullBody_ReturnsUploadExerciseImageFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json"),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.UploadExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            new StagedImage([1, 2, 3], "image/png"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<UploadExerciseImageFailed>(outcome);
    }

    [Fact]
    public async Task UploadExerciseImageAsync_ServerReturnsMalformedId_ReturnsUploadExerciseImageFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"not-a-valid-id","name":"Bodyweight Squat","imageUrl":null,"createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.UploadExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            new StagedImage([1, 2, 3], "image/png"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<UploadExerciseImageFailed>(outcome);
    }

    [Fact]
    public async Task UploadExerciseImageAsync_NetworkFailure_ReturnsUploadExerciseImageFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new HttpRequestException("network down");
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.UploadExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            new StagedImage([1, 2, 3], "image/png"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<UploadExerciseImageFailed>(outcome);
    }

    [Fact]
    public async Task UploadExerciseImageAsync_HttpClientThrowsOperationCanceledException_ReturnsUploadExerciseImageFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new OperationCanceledException("canceled");
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.UploadExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            new StagedImage([1, 2, 3], "image/png"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<UploadExerciseImageFailed>(outcome);
    }

    [Fact]
    public async Task UploadExerciseImageAsync_ImageIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.UploadExerciseImageAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                null!,
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task DeleteExerciseImageAsync_ServerReturns204_ReturnsDeleteExerciseImageSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.DeleteExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteExerciseImageSucceeded>(outcome);
        Assert.Equal(HttpMethod.Delete, _handler.LastMethod);
        Assert.Equal("/api/exercises/EXR-AAAAAA/image", _handler.LastRequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task DeleteExerciseImageAsync_ServerReturns404_ReturnsDeleteExerciseImageSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"exercise has no image"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.DeleteExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteExerciseImageSucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteExerciseImageAsync_ServerReturns403WithEmptyBody_ReturnsDeleteExerciseImageFailedWithStatusFallback()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Forbidden);
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.DeleteExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<DeleteExerciseImageFailed>(outcome);
        Assert.Contains("403", failed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteExerciseImageAsync_NetworkFailure_ReturnsDeleteExerciseImageFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new HttpRequestException("network down");
        var client = new ExercisesApiClient(_httpClient, NullLogger<ExercisesApiClient>.Instance);

        // Act
        var outcome = await client.DeleteExerciseImageAsync(
            ExerciseId.Parse("EXR-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteExerciseImageFailed>(outcome);
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage? NextResponse { get; set; }
        public Exception? NextException { get; set; }
        public HttpMethod? LastMethod { get; private set; }
        public Uri? LastRequestUri { get; private set; }
        public string? LastContentType { get; private set; }
        public byte[]? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            LastMethod = request.Method;
            LastRequestUri = request.RequestUri;
            LastContentType = request.Content?.Headers.ContentType?.MediaType;
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsByteArrayAsync(cancellationToken);

            return NextException is not null
                ? throw NextException
                : NextResponse ?? new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
