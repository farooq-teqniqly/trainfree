using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Trainfree.Web;
using Trainfree.Web.Admin;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// "/api/" resolves same-origin in production; appsettings.Development.json overrides it
// with wrangler dev's absolute local address. See CLAUDE.md: "Prod API URL is never configured."
var apiBaseAddress = builder.Configuration["Api:BaseAddress"] ?? "/api/";
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), apiBaseAddress),
});
builder.Services.AddScoped<IProgramsApiClient, ProgramsApiClient>();

await builder.Build().RunAsync();
