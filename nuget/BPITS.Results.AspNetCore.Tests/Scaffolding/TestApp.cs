using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BPITS.Results.AspNetCore.Tests.Scaffolding;

/// <summary>
/// Hosts a real ASP.NET Core pipeline in memory so tests exercise routing, endpoint metadata and response
/// writing exactly as an app would, rather than calling ExecuteAsync on a hand-built HttpContext.
/// </summary>
internal static class TestApp
{
    public static async Task<WebApplication> StartAsync(
        Action<WebApplication> mapEndpoints,
        Action<IServiceCollection>? configureServices = null,
        bool registerMapper = true)
    {
        // Production keeps the developer exception page out of the pipeline, so failures surface as exceptions
        // the test can assert on instead of HTML error pages.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });

        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        builder.Services.AddSingleton<UserService>();

        if (registerMapper)
            builder.Services.AddTestStatusCodeActionResultMapper();

        configureServices?.Invoke(builder.Services);

        var app = builder.Build();
        mapEndpoints(app);

        await app.StartAsync();
        return app;
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
