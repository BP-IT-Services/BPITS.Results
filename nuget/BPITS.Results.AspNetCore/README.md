# BPITS.Results.AspNetCore

ASP.NET Core extensions for BPITS.Results that let `ApiResult` be returned from controllers (`IActionResult`) and Minimal API endpoints (`IResult`), with automatic HTTP status code mapping.

## Features

- **Direct IActionResult Implementation** - `ApiResult` directly implements `IActionResult` and can be returned from ASP.NET Core controllers
- **Minimal API Support** - `ApiResult` also implements `IResult`, respects `ConfigureHttpJsonOptions`, and documents its mapped status codes in OpenAPI (ASP.NET Core 8+)
- **Attribute-Based HTTP Status Code Mapping** - Define HTTP status codes using attributes on your enum values
- **Source Generated** - Zero runtime overhead with compile-time code generation
- **Simple Setup** - Three-step configuration with automatic mapper registration

## Installation

```bash
dotnet add package BPITS.Results
dotnet add package BPITS.Results.AspNetCore
```

## Quick Start

### 1. Define Your Status Enum with ASP.NET Core Attributes

```csharp
using System.Net;
using BPITS.Results.Abstractions;
using BPITS.Results.AspNetCore.Abstractions;

[GenerateApiResult]
[GenerateServiceResult]
[EnableApiResultMapping]
public enum MyApiStatus
{
    [HttpStatusCode(HttpStatusCode.OK)]
    Ok = 0,

    [HttpStatusCode(HttpStatusCode.BadRequest)]
    BadRequest = 400,

    [HttpStatusCode(HttpStatusCode.NotFound)]
    NotFound = 404,

    [HttpStatusCode(HttpStatusCode.InternalServerError)]
    InternalServerError = 500
}
```

### 2. Register the Mapper in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the generated mapper
builder.Services.AddMyApiStatusActionResultMapper();

var app = builder.Build();
app.MapControllers();
app.Run();
```

### 3. Return ApiResult from Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public ApiResult<UserDto> GetUser(int id)
    {
        var result = _userService.GetUser(id);
        return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
        // Automatically returns correct HTTP status code
    }
}
```

### Or Return ApiResult from Minimal API Endpoints

```csharp
app.MapGet("/api/users/{id:int}", (int id, IUserService userService) =>
{
    var result = userService.GetUser(id);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
    // Same status code mapping as the controller version
});
```

Minimal APIs need the mapper from step 2 to be registered. See the
[Minimal API Patterns guide](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/guides/minimal-api-patterns.md)
for JSON options, OpenAPI, and how endpoints differ from controllers.

## How It Works

When you apply `[EnableApiResultMapping]` to your enum, the source generator:

1. Generates a partial `ApiResult<TEnum>` class implementing `IActionResult`, and `IResult` when your project targets ASP.NET Core 7 or later
2. Creates an HTTP status code mapper based on `[HttpStatusCode]` attributes
3. Provides a DI extension method to register the mapper
4. Automatically maps your custom status codes to HTTP status codes

## Why Use Custom Status Codes?

Custom status codes enable consuming applications to handle errors programmatically:

- **Programmatic error handling** - Clients switch on status codes to implement appropriate logic (retry, redirect, fallback)
- **No string parsing** - Handle errors based on enum values, not parsing error messages
- **Domain-driven** - Error codes match your business domain
- **Consistent semantics** - Same codes across backend and frontend
- **Flexibility** - Change HTTP mapping without affecting client logic
- **Better UX** - Proper error handling enables appropriate user experiences

## Example Response

### Success (HTTP 200)
```json
{
  "value": { "id": 123, "name": "John" },
  "isSuccess": true,
  "statusCode": 0
}
```

### Failure (HTTP 404)
```json
{
  "value": null,
  "isSuccess": false,
  "statusCode": 404,
  "errorMessage": "User not found"
}
```

## Documentation

For complete setup and usage instructions:

- **[ASP.NET Core Integration Guide](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/guides/aspnetcore-integration.md)** - Complete setup guide
- **[HTTP Status Mapping](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/advanced/http-status-mapping.md)** - Understanding the mapping mechanism
- **[Custom Status Codes](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/advanced/custom-status-codes.md)** - Configuring status codes
- **[Controller Patterns](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/guides/controller-patterns.md)** - Patterns for controllers
- **[Minimal API Patterns](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/guides/minimal-api-patterns.md)** - Patterns for Minimal API endpoints
- **[Full Documentation](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/README.md)** - Complete documentation hub

## Core Package

This package extends [BPITS.Results](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/BPITS.Results/README.md). See the core package for information about ServiceResult and ApiResult fundamentals.

## Repository

[GitHub Repository](https://github.com/BP-IT-Services/BPITS.Results)

## License

MIT License - see the [LICENSE](https://github.com/BP-IT-Services/BPITS.Results/blob/main/LICENSE) file for details.
