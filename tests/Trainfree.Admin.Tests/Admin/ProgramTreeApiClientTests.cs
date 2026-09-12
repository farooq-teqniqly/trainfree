using System.Net;
using System.Text;
using Trainfree.Admin.Admin;
using Trainfree.Domain.Ids;
using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Tests.Admin;

public sealed class ProgramTreeApiClientTests : IDisposable
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;

    public ProgramTreeApiClientTests() =>
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://worker/api/") };

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    private ProgramTreeApiClient CreateClient() => new(_httpClient);

    [Fact]
    public async Task GetProgramTreeAsync_ServerReturnsEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        };
        var client = CreateClient();

        // Act
        var result = await client.GetProgramTreeAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProgramTreeAsync_ServerReturnsFullyNestedTree_ReturnsMappedDomainTypes()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                [
                    {
                        "id": "PRG-AAAAAA",
                        "name": "Workout A",
                        "sessions": [
                            {
                                "id": "SNN-AAAAAA",
                                "programId": "PRG-AAAAAA",
                                "name": "Monday Lower Body",
                                "phases": [
                                    {
                                        "id": "SPH-AAAAAA",
                                        "sessionId": "SNN-AAAAAA",
                                        "phaseId": "PHS-AAAAAA",
                                        "exercises": [
                                            {"id":"PGX-AAAAAA","sessionPhaseId":"SPH-AAAAAA","exerciseId":"EXR-AAAAAA","type":"Reps","reps":10,"weight":45,"sets":3,"restSeconds":60,"side":"Both"}
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
                """,
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act
        var result = await client.GetProgramTreeAsync(CancellationToken.None);

        // Assert
        var program = Assert.Single(result);
        Assert.Equal(ProgramId.Parse("PRG-AAAAAA"), program.Program.Id);
        var session = Assert.Single(program.Sessions);
        Assert.Equal(SessionId.Parse("SNN-AAAAAA"), session.Session.Id);
        var phase = Assert.Single(session.Phases);
        Assert.Equal(SessionPhaseId.Parse("SPH-AAAAAA"), phase.SessionPhase.Id);
        var exercise = Assert.Single(phase.Exercises);
        var reps = Assert.IsType<RepsProgramExercise>(exercise);
        Assert.Equal(10, reps.Reps);
    }

    [Fact]
    public async Task GetProgramTreeAsync_ProgramHasNoSessions_PreservesEmptyBranch()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """[{"id":"PRG-AAAAAA","name":"Workout A","sessions":[]}]""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act
        var result = await client.GetProgramTreeAsync(CancellationToken.None);

        // Assert
        var program = Assert.Single(result);
        Assert.Empty(program.Sessions);
    }

    [Fact]
    public async Task GetProgramTreeAsync_ServerOmitsSessionsField_TreatsMissingFieldAsEmptyBranch()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """[{"id":"PRG-AAAAAA","name":"Workout A"}]""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act
        var result = await client.GetProgramTreeAsync(CancellationToken.None);

        // Assert
        var program = Assert.Single(result);
        Assert.Empty(program.Sessions);
    }

    [Fact]
    public async Task GetProgramTreeAsync_ServerReturnsNullExerciseElement_ThrowsArgumentNullException()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                [
                    {
                        "id": "PRG-AAAAAA",
                        "name": "Workout A",
                        "sessions": [
                            {
                                "id": "SNN-AAAAAA",
                                "programId": "PRG-AAAAAA",
                                "name": "Monday Lower Body",
                                "phases": [
                                    {
                                        "id": "SPH-AAAAAA",
                                        "sessionId": "SNN-AAAAAA",
                                        "phaseId": "PHS-AAAAAA",
                                        "exercises": [null]
                                    }
                                ]
                            }
                        ]
                    }
                ]
                """,
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.GetProgramTreeAsync(CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetProgramTreeAsync_ServerReturnsUnknownExerciseType_ThrowsFormatException()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                [
                    {
                        "id": "PRG-AAAAAA",
                        "name": "Workout A",
                        "sessions": [
                            {
                                "id": "SNN-AAAAAA",
                                "programId": "PRG-AAAAAA",
                                "name": "Monday Lower Body",
                                "phases": [
                                    {
                                        "id": "SPH-AAAAAA",
                                        "sessionId": "SNN-AAAAAA",
                                        "phaseId": "PHS-AAAAAA",
                                        "exercises": [
                                            {"id":"PGX-AAAAAA","sessionPhaseId":"SPH-AAAAAA","exerciseId":"EXR-AAAAAA","type":"Bogus","weight":0,"sets":3,"restSeconds":60,"side":"Both"}
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
                """,
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act / Assert
        await Assert.ThrowsAsync<FormatException>(() =>
            client.GetProgramTreeAsync(CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetProgramTreeAsync_ServerReturnsUnknownSide_ThrowsArgumentException()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                [
                    {
                        "id": "PRG-AAAAAA",
                        "name": "Workout A",
                        "sessions": [
                            {
                                "id": "SNN-AAAAAA",
                                "programId": "PRG-AAAAAA",
                                "name": "Monday Lower Body",
                                "phases": [
                                    {
                                        "id": "SPH-AAAAAA",
                                        "sessionId": "SNN-AAAAAA",
                                        "phaseId": "PHS-AAAAAA",
                                        "exercises": [
                                            {"id":"PGX-AAAAAA","sessionPhaseId":"SPH-AAAAAA","exerciseId":"EXR-AAAAAA","type":"Reps","reps":10,"weight":0,"sets":3,"restSeconds":60,"side":"Bogus"}
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
                """,
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = CreateClient();

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.GetProgramTreeAsync(CancellationToken.None)
        );
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
