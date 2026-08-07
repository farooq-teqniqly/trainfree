using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Trainfree.Web.Versioning;

namespace Trainfree.Web.Tests.Versioning;

public sealed class VersionCheckTests : IDisposable
{
    private static readonly VersionStamp ClientStamp = new("v0.0.3", "e4f5g6h");

    private readonly TestHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;

    public VersionCheckTests() =>
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://worker/api/") };

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task CheckAsync_ServerReportsSameStamp_ReturnsRunningLatestVersion()
    {
        // Arrange
        _handler.NextResponse = JsonResponse("""{"version":"v0.0.3","commit":"e4f5g6h"}""");
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<RunningLatestVersion>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ServerReportsNewerStamp_ReturnsRunningStaleVersionWithServerStamp()
    {
        // Arrange
        _handler.NextResponse = JsonResponse("""{"version":"v0.0.4","commit":"1234abc"}""");
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        var stale = Assert.IsType<RunningStaleVersion>(outcome);
        Assert.Equal("v0.0.4", stale.Deployed.Version);
    }

    [Fact]
    public async Task CheckAsync_SameVersionDifferentCommit_ReturnsRunningStaleVersion()
    {
        // Arrange
        _handler.NextResponse = JsonResponse("""{"version":"v0.0.3","commit":"1234abc"}""");
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<RunningStaleVersion>(outcome);
    }

    [Fact]
    public async Task CheckAsync_RequestFails_ReturnsVersionUnknown()
    {
        // Arrange
        _handler.Exception = new HttpRequestException("network down");
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<VersionUnknown>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ResponseIsNotJson_ReturnsVersionUnknown()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "<html>Cloudflare Access login</html>",
                Encoding.UTF8,
                "text/html"
            ),
        };
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<VersionUnknown>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ResponseCharsetIsUnresolvable_ReturnsVersionUnknown()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            "application/json; charset=made-up-charset"
        );
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<VersionUnknown>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ServerReturnsNullBody_ReturnsVersionUnknown()
    {
        // Arrange
        _handler.NextResponse = JsonResponse("null");
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<VersionUnknown>(outcome);
    }

    [Fact]
    public async Task CheckAsync_UnstampedLocalBuild_ReturnsVersionUnknown()
    {
        // Arrange
        _handler.NextResponse = JsonResponse("""{"version":"local","commit":"local"}""");
        var check = new VersionCheck(
            _httpClient,
            new VersionStamp("1.0.0", "local"),
            NullLogger<VersionCheck>.Instance
        );

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<VersionUnknown>(outcome);
    }

    private VersionCheck CreateCheck() =>
        new(_httpClient, ClientStamp, NullLogger<VersionCheck>.Instance);

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage? NextResponse { get; set; }

        public Exception? Exception { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Exception is not null
                ? Task.FromException<HttpResponseMessage>(Exception)
                : Task.FromResult(NextResponse ?? new HttpResponseMessage(HttpStatusCode.OK));
    }
}
