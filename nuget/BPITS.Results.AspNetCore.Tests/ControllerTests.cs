using System.Net;
using BPITS.Results.AspNetCore.Tests.Scaffolding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BPITS.Results.AspNetCore.Tests;

/// <summary>
/// The controller (IActionResult) path predates Minimal API support and must keep behaving the same now that
/// ApiResult also implements IResult.
/// </summary>
public class ControllerTests
{
    private static void MapControllers(WebApplication app) => app.MapControllers();

    private static void AddControllers(IServiceCollection services)
        => services.AddControllers().AddApplicationPart(typeof(ControllerTests).Assembly);

    [Fact]
    public async Task NotFound_ReturnsMapped404()
    {
        await using var app = await TestApp.StartAsync(MapControllers, AddControllers);
        var client = app.GetTestClient();

        var response = await client.GetAsync($"/api/controller-users/{Guid.NewGuid()}");
        var body = await response.ReadJsonAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal((int)TestStatusCode.ResourceNotFound, body.GetProperty("statusCode").GetInt32());
        Assert.Equal("User not found", body.GetProperty("errorMessage").GetString());
    }

    [Fact]
    public async Task WithoutRegisteredMapper_StillFallsBackToAttributeMapping()
    {
        // Controllers have always worked without registering the mapper; that must not regress.
        await using var app = await TestApp.StartAsync(MapControllers, AddControllers, registerMapper: false);
        var client = app.GetTestClient();

        var response = await client.GetAsync($"/api/controller-users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResponseBody_MatchesMinimalApiResponse(bool existingUser)
    {
        var id = existingUser ? UserService.ExistingUserId : Guid.NewGuid();

        await using var app = await TestApp.StartAsync(
            a =>
            {
                a.MapControllers();
                a.MapGet("/api/minimal-users/{id:guid}", async (Guid id, UserService userService) =>
                {
                    var result = await userService.GetUserAsync(id);
                    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
                });
            },
            AddControllers);
        var client = app.GetTestClient();

        var controllerResponse = await client.GetAsync($"/api/controller-users/{id}");
        var minimalResponse = await client.GetAsync($"/api/minimal-users/{id}");

        Assert.Equal(controllerResponse.StatusCode, minimalResponse.StatusCode);
        Assert.Equal(
            (await controllerResponse.ReadJsonAsync()).GetRawText(),
            (await minimalResponse.ReadJsonAsync()).GetRawText());
    }
}

[ApiController]
[Route("api/controller-users")]
public class ControllerUsersController : ControllerBase
{
    private readonly UserService _userService;

    public ControllerUsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ApiResult<UserDto>> GetUser(Guid id)
    {
        var result = await _userService.GetUserAsync(id);
        return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
    }
}
