# Minimal API Patterns

Patterns for returning `ApiResult` from ASP.NET Core Minimal API endpoints.

## Table of Contents

- [Overview](#overview)
- [Requirements](#requirements)
- [Setup](#setup)
- [Basic Endpoints](#basic-endpoints)
- [Paged Results](#paged-results)
- [JSON Serialization Options](#json-serialization-options)
- [OpenAPI](#openapi)
- [Differences from Controllers](#differences-from-controllers)
- [Best Practices](#best-practices)

## Overview

With `[EnableApiResultMapping]` on your status enum, the generated `ApiResult` and `ApiResult<T>` implement both
`IActionResult` (controllers) and `IResult` (Minimal APIs). Return them straight from an endpoint and they set the
HTTP status code from your `[HttpStatusCode]` attributes, the same as they do from a controller action.

The flow is the same as with controllers:

1. Use **ServiceResult** in your service layer
2. Convert to **ApiResult** in the endpoint
3. Transform entities to DTOs during conversion
4. Let ApiResult write the HTTP response

## Requirements

| Feature | Requires |
|---|---|
| Returning `ApiResult` from endpoints (`IResult`) | ASP.NET Core 7 or later |
| OpenAPI response metadata (`IEndpointMetadataProvider`) | ASP.NET Core 8 or later, C# 11 or later |

The generator checks what your project references. If those types aren't available (for example in a class library
that only references the older netstandard ASP.NET Core packages), it skips the Minimal API code and leaves the
controller support as it was.

## Setup

### Step 1: Define your status enum

This is the same enum you'd use with controllers. See the
[ASP.NET Core Integration Guide](aspnetcore-integration.md#step-1-define-your-status-enum-with-aspnet-core-attributes).

```csharp
using System.Net;
using BPITS.Results.Abstractions;
using BPITS.Results.AspNetCore.Abstractions;

[GenerateApiResult]
[GenerateServiceResult]
[EnableApiResultMapping]
public enum MyAppStatus
{
    [HttpStatusCode(HttpStatusCode.OK)]
    Ok = 0,

    [HttpStatusCode(HttpStatusCode.BadRequest)]
    BadRequest = 1,

    [HttpStatusCode(HttpStatusCode.NotFound)]
    NotFound = 2,

    [HttpStatusCode(HttpStatusCode.InternalServerError)]
    InternalServerError = 3
}
```

### Step 2: Register the mapper

**This is required for Minimal APIs.** If no mapper is registered, the first request that returns an `ApiResult`
throws an `InvalidOperationException` that names the missing registration.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMyAppStatusActionResultMapper();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

app.MapUserEndpoints();

app.Run();
```

### Step 3: Return ApiResult from endpoints

```csharp
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/users");

        users.MapGet("/{id:guid}", async (Guid id, IUserService userService) =>
        {
            var result = await userService.GetUserAsync(id);
            return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
        });

        return app;
    }
}
```

## Basic Endpoints

These are the endpoint versions of the examples in [Controller Patterns](controller-patterns.md#basic-controller-actions).
Each pair does the same thing and produces the same status code and body.

### GET - Retrieve Single Item

Controller:

```csharp
[HttpGet("{id}")]
public async Task<ApiResult<UserDto>> GetUser(Guid id)
{
    var result = await _userService.GetUserAsync(id);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}
```

Minimal API:

```csharp
users.MapGet("/{id:guid}", async (Guid id, IUserService userService) =>
{
    var result = await userService.GetUserAsync(id);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
});
```

### GET - Retrieve List

```csharp
users.MapGet("/", async (IUserService userService) =>
{
    var result = await userService.GetAllUsersAsync();

    return ApiResult.FromServiceResult(
        result.MapValue(list => list?.Select(u => u.ToDto()).ToList())
    );
});
```

### POST - Create Resource

```csharp
users.MapPost("/", async (CreateUserRequest request, IUserService userService) =>
{
    var result = await userService.CreateUserAsync(request);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
});
```

If the service returns `ServiceResult.ValidationFailure(...)`, the response is a 400 with the field errors in
`errorDetails`. See [Validation Patterns](validation-patterns.md).

### PUT - Update Resource

```csharp
users.MapPut("/{id:guid}", async (Guid id, UpdateUserRequest request, IUserService userService) =>
{
    var result = await userService.UpdateUserAsync(id, request);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
});
```

### DELETE - Remove Resource

```csharp
users.MapDelete("/{id:guid}", async (Guid id, IUserService userService) =>
{
    // Non-generic ApiResult (no value returned)
    var result = await userService.DeleteUserAsync(id);
    return ApiResult.FromServiceResult(result);
});
```

### Handler methods instead of lambdas

For larger endpoints, use a static method. The return type works the same way:

```csharp
users.MapGet("/{id:guid}", GetUser);

static async Task<ApiResult<UserDto>> GetUser(Guid id, IUserService userService)
{
    var result = await userService.GetUserAsync(id);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}
```

## Paged Results

```csharp
users.MapGet("/paged", async (IUserService userService, int pageNumber = 1, int pageSize = 10) =>
{
    var result = await userService.GetUsersPagedAsync(pageNumber, pageSize);

    return ApiResult.FromServiceResult(
        result.MapValue(paged => paged == null ? null : new PagedResult<UserDto>
        {
            Items = paged.Items.Select(u => u.ToDto()).ToList(),
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        })
    );
});
```

## JSON Serialization Options

Minimal APIs take their JSON settings from `Microsoft.AspNetCore.Http.Json.JsonOptions`, configured with
`ConfigureHttpJsonOptions`. `ApiResult` uses those settings when it writes the response:

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Write status codes as names ("NotFound") instead of numbers
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

Controllers use a separate setting, `AddControllers().AddJsonOptions(...)`. If your app has both controllers and
endpoints and you want the same JSON from each, configure both.

With the default options, a response looks like this:

```json
{
  "statusCode": 2,
  "value": null,
  "errorMessage": "User not found",
  "errorDetails": null
}
```

## OpenAPI

On ASP.NET Core 8 and later, endpoints that return `ApiResult` add OpenAPI response metadata themselves. Every distinct
status code from your `[HttpStatusCode]` attributes is listed, so you don't need `.Produces(...)` calls:

```csharp
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.MapUserEndpoints(); // responses 200, 400, 404, 500 documented automatically
```

Some details about the document:

- **Schema name.** ApiExplorer ignores response types that implement `IResult`, so the generator documents a
  matching record, `ApiResultResponse<T>`, which has the same JSON shape. It appears in the document as, for example,
  `ApiResultResponseOfUserDto`. Don't use this type in application code.
- **Status codes come from the attributes.** If you register a custom `IActionResultMapper<TEnum>` that maps codes
  differently, the document still lists the attribute-based codes.
- **`.Produces<ApiResult<T>>(...)` is ignored**, for the same `IResult` reason. You don't need it.

## Differences from Controllers

| | Controllers | Minimal APIs |
|---|---|---|
| Interface used | `IActionResult` | `IResult` |
| Mapper registration | Optional (falls back to the attribute mapping) | **Required** (throws if missing) |
| JSON options | `AddControllers().AddJsonOptions(...)` | `ConfigureHttpJsonOptions(...)` |
| OpenAPI responses | Add `[ProducesResponseType]` yourself | Generated from `[HttpStatusCode]` |

You can use both in the same app. The same `ApiResult` type works in either place.

## Best Practices

### 1. Return ApiResult directly

```csharp
// Good: ApiResult is the IResult
users.MapGet("/{id:guid}", async (Guid id, IUserService userService) =>
    ApiResult.FromServiceResult((await userService.GetUserAsync(id)).MapValue(u => u?.ToDto())));

// Bad: wrapping it loses the mapped status code (always 200)
users.MapGet("/{id:guid}", async (Guid id, IUserService userService) =>
    Results.Ok(ApiResult.FromServiceResult((await userService.GetUserAsync(id)).MapValue(u => u?.ToDto()))));
```

### 2. Convert ServiceResult to ApiResult in the endpoint

As with controllers, don't return `ServiceResult` from an endpoint. It isn't an `IResult`, so it is serialized with a
200, and it can expose exception details.

### 3. Keep endpoints thin

Validation and business logic belong in the service. The endpoint converts the result and maps it to DTOs.

## Related

- [Controller Patterns](controller-patterns.md) - The same patterns for controllers
- [ASP.NET Core Integration](aspnetcore-integration.md) - Enum setup and status code mapping
- [Validation Patterns](validation-patterns.md) - Field-level validation errors
- [HTTP Status Mapping](../advanced/http-status-mapping.md) - How the mapping works
