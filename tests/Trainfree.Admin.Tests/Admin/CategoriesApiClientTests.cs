using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Trainfree.Admin.Admin;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Admin;

public sealed class CategoriesApiClientTests : IDisposable
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;

    public CategoriesApiClientTests() =>
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://worker/api/") };

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task GetCategoriesAsync_ServerReturns200_ReturnsMappedCategories()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                [
                    {"id":"CAT-AAAAAA","name":"Warm Up","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"},
                    {"id":"CAT-BBBBBB","name":"Cool Down","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}
                ]
                """,
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var categories = await client.GetCategoriesAsync(CancellationToken.None);

        // Assert
        Assert.Collection(
            categories,
            c =>
            {
                Assert.Equal(CategoryId.Parse("CAT-AAAAAA"), c.Id);
                Assert.Equal("Warm Up", c.Name);
            },
            c =>
            {
                Assert.Equal(CategoryId.Parse("CAT-BBBBBB"), c.Id);
                Assert.Equal("Cool Down", c.Name);
            }
        );
    }

    [Fact]
    public async Task GetCategoriesAsync_ServerReturnsEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
        };
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var categories = await client.GetCategoriesAsync(CancellationToken.None);

        // Assert
        Assert.Empty(categories);
    }

    [Fact]
    public async Task CreateCategoryAsync_ServerReturnsTheAccessLoginPage_ReturnsCreateCategoryFailed()
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
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var outcome = await client.CreateCategoryAsync("New Category", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreateCategoryFailed>(outcome);
        Assert.Contains("302", failed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateCategoryAsync_ErrorBodyIsMalformedJson_ReturnsCreateCategoryFailed()
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
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var outcome = await client.CreateCategoryAsync("New Category", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreateCategoryFailed>(outcome);
        Assert.Contains("502", failed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenameCategoryAsync_ServerReturns200_ReturnsRenameCategorySucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"CAT-AAAAAA","name":"Renamed","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var outcome = await client.RenameCategoryAsync(
            CategoryId.Parse("CAT-AAAAAA"),
            "Renamed",
            CancellationToken.None
        );

        // Assert
        var succeeded = Assert.IsType<RenameCategorySucceeded>(outcome);
        Assert.Equal("Renamed", succeeded.Category.Name);
    }

    [Fact]
    public async Task RenameCategoryAsync_ServerReturns400_ReturnsRenameCategoryFailedWithServerError()
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
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var outcome = await client.RenameCategoryAsync(
            CategoryId.Parse("CAT-AAAAAA"),
            "Ab",
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<RenameCategoryFailed>(outcome);
        Assert.Equal("name must be between 4 and 100 characters", failed.Error);
    }

    [Fact]
    public async Task RenameCategoryAsync_ServerReturns404_ReturnsRenameCategoryFailed()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"category not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var outcome = await client.RenameCategoryAsync(
            CategoryId.Parse("CAT-AAAAAA"),
            "Renamed",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<RenameCategoryFailed>(outcome);
    }

    [Fact]
    public async Task CreateCategoryAsync_ServerReturns201_ReturnsCreateCategorySucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                """{"id":"CAT-AAAAAA","name":"New Category","createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var outcome = await client.CreateCategoryAsync("New Category", CancellationToken.None);

        // Assert
        var succeeded = Assert.IsType<CreateCategorySucceeded>(outcome);
        Assert.Equal("New Category", succeeded.Category.Name);
    }

    [Fact]
    public async Task CreateCategoryAsync_ServerReturns409_ReturnsCreateCategoryFailedWithServerError()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                """{"error":"A category named \"New Category\" already exists."}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var outcome = await client.CreateCategoryAsync("New Category", CancellationToken.None);

        // Assert
        var failed = Assert.IsType<CreateCategoryFailed>(outcome);
        Assert.Equal("A category named \"New Category\" already exists.", failed.Error);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ServerReturns204_ReturnsDeleteCategorySucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var outcome = await client.DeleteCategoryAsync(
            CategoryId.Parse("CAT-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DeleteCategorySucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ServerReturns404_ReturnsDeleteCategorySucceeded()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"error":"category not found"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var outcome = await client.DeleteCategoryAsync(
            CategoryId.Parse("CAT-AAAAAA"),
            CancellationToken.None
        );

        // Assert -- a 404 means the caller's desired end state already holds, so it is
        // treated as success rather than surfaced as an error.
        Assert.IsType<DeleteCategorySucceeded>(outcome);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ServerReturns500_ReturnsDeleteCategoryFailedWithoutThrowing()
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
        var client = new CategoriesApiClient(_httpClient, NullLogger<CategoriesApiClient>.Instance);

        // Act
        var outcome = await client.DeleteCategoryAsync(
            CategoryId.Parse("CAT-AAAAAA"),
            CancellationToken.None
        );

        // Assert
        var failed = Assert.IsType<DeleteCategoryFailed>(outcome);
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
