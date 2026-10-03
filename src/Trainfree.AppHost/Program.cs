// Local-dev orchestration only. Ports are fixed because the Blazor client reads its API base
// address from wwwroot/appsettings.Development.json, a static file Aspire cannot inject into.
// The PORT env var on each endpoint is only Aspire's URL/health bookkeeping; wrangler ignores
// it and listens on the dev.port pinned in its own wrangler.jsonc.
var builder = DistributedApplication.CreateBuilder(args);

var adminApi = builder
    .AddJavaScriptApp("admin-api", "../Trainfree.AdminApi", "dev")
    .WithNpm(install: false)
    .WithHttpEndpoint(port: 9999, env: "PORT", isProxied: false)
    .WithHttpHealthCheck("/api/version");

if (
    bool.TryParse(builder.Configuration["Trainfree:IdentityApi"], out var identityApiEnabled)
    && identityApiEnabled
)
{
    builder
        .AddJavaScriptApp("identity-api", "../Trainfree.IdentityApi", "dev")
        .WithNpm(install: false)
        .WithHttpEndpoint(port: 9998, env: "PORT", isProxied: false)
        .WithHttpHealthCheck("/api/version");
}

builder.AddProject<Projects.Trainfree_Admin>("admin", "http").WaitFor(adminApi);

await builder.Build().RunAsync();
