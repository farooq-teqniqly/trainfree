using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Trainfree.Admin.Admin;

namespace Trainfree.Admin.Tests.Admin;

public sealed class AccessCheckTests : IDisposable
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;

    public AccessCheckTests() =>
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://worker/api/") };

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task CheckAsync_ServerReturns200WithAdministratorRole_ReturnsAdministrator()
    {
        // Arrange
        _handler.NextResponse = JsonResponse(
            HttpStatusCode.OK,
            """{"email":"a@x.com","role":"Administrator"}"""
        );
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<Administrator>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ServerReturns200WithNonAdministratorRole_ReturnsNoAccess()
    {
        // Arrange
        _handler.NextResponse = JsonResponse(
            HttpStatusCode.OK,
            """{"email":"u@x.com","role":"User"}"""
        );
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<NoAccess>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ServerReturns401_ReturnsNoAccess()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<NoAccess>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ServerReturns403_ReturnsNoAccess()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.Forbidden);
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<NoAccess>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ServerReturns503_ReturnsAccessCheckFailed()
    {
        // Arrange
        _handler.NextResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<AccessCheckFailed>(outcome);
    }

    [Fact]
    public async Task CheckAsync_RequestFails_ReturnsAccessCheckFailed()
    {
        // Arrange
        _handler.NextException = new HttpRequestException("network down");
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<AccessCheckFailed>(outcome);
    }

    [Fact]
    public async Task CheckAsync_RequestTimesOut_ReturnsAccessCheckFailed()
    {
        // Arrange
        _handler.NextException = new TaskCanceledException(
            "The request was canceled due to the configured HttpClient.Timeout."
        );
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<AccessCheckFailed>(outcome);
    }

    [Fact]
    public async Task CheckAsync_CallerCancels_PropagatesTheCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _handler.NextException = new TaskCanceledException("canceled");
        var check = CreateCheck();

        // Act / Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => check.CheckAsync(cts.Token));
    }

    [Fact]
    public async Task CheckAsync_ResponseIsNotJson_ReturnsReauthenticationRequired()
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
        Assert.IsType<ReauthenticationRequired>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ServerReturnsNullBody_ReturnsReauthenticationRequired()
    {
        // Arrange
        _handler.NextResponse = JsonResponse(HttpStatusCode.OK, "null");
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<ReauthenticationRequired>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ServerReturnsEmptyObject_ReturnsReauthenticationRequired()
    {
        // Arrange
        _handler.NextResponse = JsonResponse(HttpStatusCode.OK, "{}");
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<ReauthenticationRequired>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ServerReturnsRoleWithoutEmail_ReturnsReauthenticationRequired()
    {
        // Arrange
        _handler.NextResponse = JsonResponse(HttpStatusCode.OK, """{"role":"Administrator"}""");
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<ReauthenticationRequired>(outcome);
    }

    [Fact]
    public async Task CheckAsync_ServerReturnsEmailWithoutRole_ReturnsReauthenticationRequired()
    {
        // Arrange
        _handler.NextResponse = JsonResponse(HttpStatusCode.OK, """{"email":"a@x.com"}""");
        var check = CreateCheck();

        // Act
        var outcome = await check.CheckAsync(CancellationToken.None);

        // Assert
        Assert.IsType<ReauthenticationRequired>(outcome);
    }

    private AccessCheck CreateCheck() => new(_httpClient, NullLogger<AccessCheck>.Instance);

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

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
