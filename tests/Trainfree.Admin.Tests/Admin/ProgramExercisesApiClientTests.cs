using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Trainfree.Admin.Admin;
using Trainfree.Domain.Ids;
using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Tests.Admin;

public sealed class ProgramExercisesApiClientTests : IDisposable
{
    private static readonly ProgramId _programId = ProgramId.Parse("PRG-AAAAAA");
    private static readonly SessionId _sessionId = SessionId.Parse("SNN-AAAAAA");
    private static readonly SessionPhaseId _sessionPhaseId = SessionPhaseId.Parse("SPH-AAAAAA");
    private static readonly ExerciseId _exerciseId = ExerciseId.Parse("EXR-AAAAAA");

    private readonly TestHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;

    public ProgramExercisesApiClientTests() =>
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://worker/api/") };

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    private ProgramExercisesApiClient CreateClient() =>
        new(_httpClient, NullLogger<ProgramExercisesApiClient>.Instance);

    [Fact]
    public async Task GetProgramExercisesAsync_ServerReturnsMixedTypes_ReturnsMappedDomainTypes()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                [
                    {"id":"PGX-AAAAAA","sessionPhaseId":"SPH-AAAAAA","exerciseId":"EXR-AAAAAA","type":"Reps","reps":10,"weight":45,"sets":3,"restSeconds":60,"side":"Both"},
                    {"id":"PGX-BBBBBB","sessionPhaseId":"SPH-AAAAAA","exerciseId":"EXR-AAAAAA","type":"Timed","durationSeconds":30,"weight":0,"sets":3,"restSeconds":60,"side":"Left"}
                ]
                """,
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act
        var result = await client.GetProgramExercisesAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            CancellationToken.None
        );

        // Assert
        Assert.Collection(
            result,
            pe =>
            {
                var reps = Assert.IsType<RepsProgramExercise>(pe);
                Assert.Equal(ProgramExerciseId.Parse("PGX-AAAAAA"), reps.Id);
                Assert.Equal(10, reps.Reps);
                Assert.Equal(45, reps.Weight);
                Assert.Equal(ProgramExerciseSide.Both, reps.Side);
            },
            pe =>
            {
                var timed = Assert.IsType<TimedProgramExercise>(pe);
                Assert.Equal(ProgramExerciseId.Parse("PGX-BBBBBB"), timed.Id);
                Assert.Equal(30, timed.DurationSeconds);
                Assert.Equal(ProgramExerciseSide.Left, timed.Side);
            }
        );
    }

    [Fact]
    public async Task GetProgramExercisesAsync_ServerReturnsEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        };
        var client = CreateClient();

        // Act
        var result = await client.GetProgramExercisesAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            CancellationToken.None
        );

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProgramExercisesAsync_ServerReturnsUnknownType_ThrowsFormatException()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """[{"id":"PGX-AAAAAA","sessionPhaseId":"SPH-AAAAAA","exerciseId":"EXR-AAAAAA","type":"Bogus","weight":0,"sets":3,"restSeconds":60,"side":"Both"}]""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act / Assert
        await Assert.ThrowsAsync<FormatException>(() =>
            client.GetProgramExercisesAsync(
                _programId,
                _sessionId,
                _sessionPhaseId,
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task GetProgramExercisesAsync_ServerReturnsNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[null]", Encoding.UTF8, "application/json"),
        };
        var client = CreateClient();

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.GetProgramExercisesAsync(
                _programId,
                _sessionId,
                _sessionPhaseId,
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task GetProgramExercisesAsync_ServerReturnsUnknownSide_ThrowsArgumentException()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """[{"id":"PGX-AAAAAA","sessionPhaseId":"SPH-AAAAAA","exerciseId":"EXR-AAAAAA","type":"Reps","reps":10,"weight":0,"sets":3,"restSeconds":60,"side":"Bogus"}]""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.GetProgramExercisesAsync(
                _programId,
                _sessionId,
                _sessionPhaseId,
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task CreateRepsProgramExerciseAsync_ServerReturns201_ReturnsSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                """{"id":"PGX-AAAAAA","sessionPhaseId":"SPH-AAAAAA","exerciseId":"EXR-AAAAAA","type":"Reps","reps":10,"weight":0,"sets":3,"restSeconds":60,"side":"Both"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act
        var outcome = await client.CreateRepsProgramExerciseAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            _exerciseId,
            10,
            new SetPrescription(3, 60),
            CancellationToken.None
        );

        // Assert
        var succeeded = Assert.IsType<CreateProgramExerciseSucceeded>(outcome);
        var reps = Assert.IsType<RepsProgramExercise>(succeeded.ProgramExercise);
        Assert.Equal(10, reps.Reps);
    }

    [Fact]
    public async Task CreateTimedProgramExerciseAsync_ServerReturns201_ReturnsSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                """{"id":"PGX-AAAAAA","sessionPhaseId":"SPH-AAAAAA","exerciseId":"EXR-AAAAAA","type":"Timed","durationSeconds":30,"weight":0,"sets":3,"restSeconds":60,"side":"Both"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act
        var outcome = await client.CreateTimedProgramExerciseAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            _exerciseId,
            30,
            new SetPrescription(3, 60),
            CancellationToken.None
        );

        // Assert
        var succeeded = Assert.IsType<CreateProgramExerciseSucceeded>(outcome);
        var timed = Assert.IsType<TimedProgramExercise>(succeeded.ProgramExercise);
        Assert.Equal(30, timed.DurationSeconds);
    }

    [Fact]
    public async Task CreateRepsProgramExerciseAsync_ServerReturns400_ReturnsFailedWithServerError()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"error":"reps must be a positive integer"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act
        var outcome = await client.CreateRepsProgramExerciseAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            _exerciseId,
            10,
            new SetPrescription(3, 60),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<CreateProgramExerciseFailed>(outcome);
        Assert.Equal("reps must be a positive integer", failed.Error);
    }

    [Fact]
    public async Task CreateRepsProgramExerciseAsync_ServerReturns404_ReturnsFailed()
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
        var client = CreateClient();

        // Act
        var outcome = await client.CreateRepsProgramExerciseAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            _exerciseId,
            10,
            new SetPrescription(3, 60),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<CreateProgramExerciseFailed>(outcome);
    }

    [Fact]
    public async Task CreateRepsProgramExerciseAsync_HttpClientThrowsJsonException_ReturnsFailedWithoutThrowing()
    {
        // Arrange
        _handler.NextException = new JsonException("malformed body");
        var client = CreateClient();

        // Act
        var outcome = await client.CreateRepsProgramExerciseAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            _exerciseId,
            10,
            new SetPrescription(3, 60),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<CreateProgramExerciseFailed>(outcome);
    }

    [Fact]
    public async Task UpdateProgramExerciseAsync_ServerReturns200_ReturnsSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"PGX-AAAAAA","sessionPhaseId":"SPH-AAAAAA","exerciseId":"EXR-AAAAAA","type":"Reps","reps":12,"weight":45,"sets":3,"restSeconds":60,"side":"Both"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act
        var outcome = await client.UpdateProgramExerciseAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            new ProgramExerciseUpdate(Reps: 12, Weight: 45),
            CancellationToken.None
        );

        // Assert
        var succeeded = Assert.IsType<UpdateProgramExerciseSucceeded>(outcome);
        var reps = Assert.IsType<RepsProgramExercise>(succeeded.ProgramExercise);
        Assert.Equal(12, reps.Reps);
        Assert.Equal(45, reps.Weight);
    }

    [Fact]
    public async Task UpdateProgramExerciseAsync_ServerReturns404_ReturnsFailed()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"program exercise not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act
        var outcome = await client.UpdateProgramExerciseAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            new ProgramExerciseUpdate(Weight: 45),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<UpdateProgramExerciseFailed>(outcome);
    }

    [Fact]
    public async Task UpdateProgramExerciseAsync_ServerReturns400_ReturnsFailedWithServerError()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"error":"weight must be a non-negative number"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act
        var outcome = await client.UpdateProgramExerciseAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            new ProgramExerciseUpdate(Weight: -1),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<UpdateProgramExerciseFailed>(outcome);
        Assert.Equal("weight must be a non-negative number", failed.Error);
    }

    [Fact]
    public async Task UpdateProgramExerciseAsync_NullUpdate_ThrowsArgumentNullException()
    {
        // Arrange
        var client = CreateClient();

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.UpdateProgramExerciseAsync(
                _programId,
                _sessionId,
                _sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                null!,
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task DeleteProgramExerciseAsync_ServerReturns204_ReturnsSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        var client = CreateClient();

        // Act
        var outcome = await client.DeleteProgramExerciseAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteProgramExerciseSucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteProgramExerciseAsync_ServerReturns404_ReturnsSucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"program exercise not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act
        var outcome = await client.DeleteProgramExerciseAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            CancellationToken.None
        );

        // Assert -- a 404 means the caller's desired end state already holds, so it is
        // treated as success rather than surfaced as an error.
        Assert.IsType<DeleteProgramExerciseSucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteProgramExerciseAsync_ServerReturns500_ReturnsFailedWithoutThrowing()
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
        var client = CreateClient();

        // Act
        var outcome = await client.DeleteProgramExerciseAsync(
            _programId,
            _sessionId,
            _sessionPhaseId,
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<DeleteProgramExerciseFailed>(outcome);
        Assert.Equal("internal error", failed.Error);
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
