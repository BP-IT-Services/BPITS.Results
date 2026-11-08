using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BPITS.Results.AspNetCore.Abstractions;
using BPITS.Results.AspNetCore.Tests.Scaffolding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BPITS.Results.AspNetCore.Tests;

public class MinimalApiTests
{
    private const string UsersRoute = "/api/users";

    private static void MapUserEndpoints(WebApplication app)
    {
        app.MapGet("/api/ping", () => ApiResult.Success());

        var users = app.MapGroup(UsersRoute);

        users.MapGet("/{id:guid}", async (Guid id, UserService userService) =>
        {
            var result = await userService.GetUserAsync(id);
            return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
        });

        users.MapPost("/", async (CreateUserRequest request, UserService userService) =>
        {
            var result = await userService.CreateUserAsync(request);
            return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
        });

        users.MapDelete("/{id:guid}", async (Guid id, UserService userService) =>
        {
            var result = await userService.DeleteUserAsync(id);
            return ApiResult.FromServiceResult(result);
        });
    }

    [Fact]
    public async Task Success_WithoutValue_Returns200AndResultEnvelope()
    {
        await using var app = await TestApp.StartAsync(MapUserEndpoints);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/ping");
        var body = await response.ReadJsonAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)TestStatusCode.Ok, body.GetProperty("statusCode").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("value").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("errorMessage").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("errorDetails").ValueKind);
    }

    [Fact]
    public async Task GenericSuccess_Returns200AndValue()
    {
        await using var app = await TestApp.StartAsync(MapUserEndpoints);
        var client = app.GetTestClient();

        var response = await client.GetAsync($"{UsersRoute}/{UserService.ExistingUserId}");
        var body = await response.ReadJsonAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((int)TestStatusCode.Ok, body.GetProperty("statusCode").GetInt32());

        var value = body.GetProperty("value");
        Assert.Equal(UserService.ExistingUserId, value.GetProperty("id").GetGuid());
        Assert.Equal("Ada Lovelace", value.GetProperty("name").GetString());
        Assert.Equal("ada@example.com", value.GetProperty("email").GetString());
    }

    [Fact]
    public async Task ValidationFailure_Returns400WithFieldErrorDetails()
    {
        await using var app = await TestApp.StartAsync(MapUserEndpoints);
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(UsersRoute, new CreateUserRequest("", "not-an-email"));
        var body = await response.ReadJsonAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal((int)TestStatusCode.BadRequest, body.GetProperty("statusCode").GetInt32());
        Assert.Equal("Validation failed", body.GetProperty("errorMessage").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("value").ValueKind);

        var errorDetails = body.GetProperty("errorDetails");
        Assert.Equal(new[] { "Name is required." }, ReadStrings(errorDetails.GetProperty("Name")));
        Assert.Equal(
            new[] { "Email is required.", "Email must be a valid address." },
            ReadStrings(errorDetails.GetProperty("Email")));
    }

    [Fact]
    public async Task NotFound_ReturnsMapped404()
    {
        await using var app = await TestApp.StartAsync(MapUserEndpoints);
        var client = app.GetTestClient();

        var response = await client.GetAsync($"{UsersRoute}/{Guid.NewGuid()}");
        var body = await response.ReadJsonAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal((int)TestStatusCode.ResourceNotFound, body.GetProperty("statusCode").GetInt32());
        Assert.Equal("User not found", body.GetProperty("errorMessage").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("value").ValueKind);
    }

    [Fact]
    public async Task NonGenericFailure_ReturnsMapped404()
    {
        await using var app = await TestApp.StartAsync(MapUserEndpoints);
        var client = app.GetTestClient();

        var response = await client.DeleteAsync($"{UsersRoute}/{Guid.NewGuid()}");
        var body = await response.ReadJsonAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("User not found", body.GetProperty("errorMessage").GetString());
    }

    [Fact]
    public async Task HttpJsonOptions_PascalCase_AreRespected()
    {
        await using var app = await TestApp.StartAsync(
            MapUserEndpoints,
            services => services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNamingPolicy = null));
        var client = app.GetTestClient();

        var response = await client.GetAsync($"{UsersRoute}/{UserService.ExistingUserId}");
        var body = await response.ReadJsonAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.TryGetProperty("StatusCode", out _));
        Assert.False(body.TryGetProperty("statusCode", out _));
        Assert.Equal("Ada Lovelace", body.GetProperty("Value").GetProperty("Name").GetString());
    }

    [Fact]
    public async Task HttpJsonOptions_Converters_AreRespected()
    {
        await using var app = await TestApp.StartAsync(
            MapUserEndpoints,
            services => services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter())));
        var client = app.GetTestClient();

        var response = await client.GetAsync($"{UsersRoute}/{Guid.NewGuid()}");
        var body = await response.ReadJsonAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(nameof(TestStatusCode.ResourceNotFound), body.GetProperty("statusCode").GetString());
    }

    [Fact]
    public async Task MvcJsonOptions_DoNotAffectMinimalApiResponses()
    {
        // Guards against reading the controller-side options by mistake: the two are configured separately.
        await using var app = await TestApp.StartAsync(
            MapUserEndpoints,
            services => services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null));
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/ping");
        var body = await response.ReadJsonAsync();

        Assert.True(body.TryGetProperty("statusCode", out _));
    }

    [Fact]
    public async Task CustomMapper_RegisteredInContainer_IsUsed()
    {
        await using var app = await TestApp.StartAsync(
            MapUserEndpoints,
            services => services.AddSingleton<IActionResultMapper<TestStatusCode>, TeapotMapper>(),
            registerMapper: false);
        var client = app.GetTestClient();

        var response = await client.GetAsync($"{UsersRoute}/{Guid.NewGuid()}");

        Assert.Equal((HttpStatusCode)418, response.StatusCode);
    }

    [Fact]
    public async Task MissingMapper_FailsWithActionableMessage()
    {
        await using var app = await TestApp.StartAsync(MapUserEndpoints, registerMapper: false);
        var client = app.GetTestClient();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/api/ping"));

        Assert.Contains("IActionResultMapper<TestStatusCode>", exception.Message);
        Assert.Contains("AddTestStatusCodeActionResultMapper()", exception.Message);
    }

    [Fact]
    public async Task EndpointMetadata_DescribesEveryMappedStatusCode()
    {
        await using var app = await TestApp.StartAsync(MapUserEndpoints);

        var endpoint = app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == $"{UsersRoute}/{{id:guid}}"
                && e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains("GET") == true);

        var responses = endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();

        var expectedStatusCodes = new[] { 200, 400, 401, 403, 404, 409, 410, 500, 502, 503 };
        Assert.Equal(expectedStatusCodes, responses.Select(r => r.StatusCode).OrderBy(c => c));
        Assert.All(responses, r =>
        {
            // ApiExplorer drops metadata whose type is an IResult, so the IResult-free mirror is documented instead.
            Assert.Equal(typeof(ApiResultResponse<UserDto>), r.Type);
            Assert.Contains("application/json", r.ContentTypes);
        });
    }

    [Fact]
    public async Task OpenApiDocument_ListsMappedResponses()
    {
        await using var app = await TestApp.StartAsync(
            a =>
            {
                MapUserEndpoints(a);
                a.MapOpenApi();
            },
            services => services.AddOpenApi());
        var client = app.GetTestClient();

        var document = await (await client.GetAsync("/openapi/v1.json")).ReadJsonAsync();
        var responses = document
            .GetProperty("paths")
            .GetProperty($"{UsersRoute}/{{id}}")
            .GetProperty("get")
            .GetProperty("responses");

        foreach (var statusCode in new[] { "200", "400", "404", "500" })
            Assert.True(responses.TryGetProperty(statusCode, out _), $"OpenAPI is missing the {statusCode} response.");

        var schemaReference = responses
            .GetProperty("404")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        var schemaName = schemaReference!.Split('/').Last();
        var schemaProperties = document
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty(schemaName)
            .GetProperty("properties");

        foreach (var property in new[] { "statusCode", "value", "errorMessage", "errorDetails" })
            Assert.True(schemaProperties.TryGetProperty(property, out _), $"Schema {schemaName} is missing {property}.");

        // Value is nullable, so it is emitted as oneOf [null, $ref] rather than a bare $ref.
        Assert.Contains("#/components/schemas/UserDto", schemaProperties.GetProperty("value").GetRawText());
    }

    private static string?[] ReadStrings(JsonElement array)
        => array.EnumerateArray().Select(e => e.GetString()).ToArray();

    private sealed class TeapotMapper : IActionResultMapper<TestStatusCode>
    {
        public HttpStatusCode MapStatusCode(TestStatusCode statusCode)
            => statusCode == TestStatusCode.Ok ? HttpStatusCode.OK : (HttpStatusCode)418;
    }
}
