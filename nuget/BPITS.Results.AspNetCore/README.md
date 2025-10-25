# BPITS.Results.AspNetCore

ASP.NET Core extensions for BPITS.Results that enable `ApiResult` to implement `IActionResult` with automatic HTTP status code mapping.

## Features

- **Direct IActionResult Implementation**: `ApiResult` directly implements `IActionResult` and can be returned from ASP.NET Core controllers
- **Attribute-Based HTTP Status Code Mapping**: Define HTTP status codes using attributes on your enum values
- **Source Generated**: Zero runtime overhead with compile-time code generation
- **Simple Setup**: Three-step configuration with automatic mapper registration

## Installation

```bash
dotnet add package BPITS.Results
dotnet add package BPITS.Results.AspNetCore
```

## Quick Start

### 1. Define Your Status Enum

Add the `IncludeActionResultMapper = true` parameter to enable ASP.NET Core integration:

```csharp
using BPITS.Results;
using BPITS.Results.AspNetCore.Abstractions;

[ResultStatusCode(IncludeActionResultMapper = true)]
public enum MyApiStatusCode
{
    Ok = 0,
    BadRequest = 400,
    ResourceNotFound = 404,
    InternalServerError = 500
}
```

### 2. Apply HTTP Status Code Attributes

Use the `[HttpStatusCode]` attribute to map enum values to HTTP status codes:

```csharp
using System.Net;

[ResultStatusCode(IncludeActionResultMapper = true)]
public enum MyApiStatusCode
{
    [HttpStatusCode(HttpStatusCode.OK)]
    Ok = 0,

    [HttpStatusCode(HttpStatusCode.BadRequest)]
    BadRequest = 400,

    [HttpStatusCode(HttpStatusCode.NotFound)]
    ResourceNotFound = 404,

    [HttpStatusCode(HttpStatusCode.InternalServerError)]
    InternalServerError = 500
}
```

### 3. Register the Mapper

In your `Program.cs`, register the generated mapper:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the generated mapper
builder.Services.AddMyApiStatusCodeActionResultMapper();

var app = builder.Build();
```

### 4. Use in Controllers

Return `ApiResult` directly from your controller actions:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public ApiResult<UserDto> GetUser(int id)
    {
        var serviceResult = _userService.GetUser(id); // Returns ServiceResult<User>

        // ApiResult implements IActionResult and can be returned directly
        return ApiResult.FromServiceResult(serviceResult.MapValue(user => user?.ToDto()));
    }

    [HttpPost]
    public ApiResult<UserDto> CreateUser([FromBody] CreateUserRequest request)
    {
        var serviceResult = _userService.CreateUser(request);
        return ApiResult.FromServiceResult(serviceResult.MapValue(user => user?.ToDto()));
    }
}
```

## How It Works

When you set `IncludeActionResultMapper = true`, the source generator:

1. **Generates a partial `ApiResult<TEnum>` class** that implements `IActionResult`
2. **Creates an HTTP status code mapper** that reads the `[HttpStatusCode]` attributes from your enum
3. **Provides an extension method** (`AddMyApiStatusCodeActionResultMapper()`) to register the mapper in DI
4. **Automatically maps** your custom status codes to HTTP status codes when the result is returned from a controller

The `ApiResult` type becomes an `IActionResult`, so ASP.NET Core's model binding automatically:
- Sets the HTTP response status code based on your mapping
- Serializes the result value to JSON (on success)
- Serializes error information (on failure)

## Understanding Custom Status Codes

### Why Use Custom Status Codes?

Custom status codes allow you to define application-specific error states that are independent of HTTP semantics. This approach provides several advantages:

- **Unified error handling** - Use the same status codes consistently across services, business logic, and API layers
- **Domain-driven design** - Define error states that match your business domain rather than being constrained by HTTP conventions
- **Easier maintenance** - Change the HTTP status code returned for a particular error by simply updating the `[HttpStatusCode]` attribute
- **Better observability** - HTTP status codes are set appropriately for logging and monitoring, while your application logic works with meaningful domain-specific codes

### Enum Values Don't Need to Match HTTP Status Codes

In the examples above, enum values like `400` and `404` are used for clarity, but **your enum values can be any numbers**. The `[HttpStatusCode]` attribute defines the actual HTTP status code returned to clients:

```csharp
[ResultStatusCode(IncludeActionResultMapper = true)]
public enum MyApiStatusCode
{
    [HttpStatusCode(HttpStatusCode.OK)]
    Success = 0,                    // Returns HTTP 200

    [HttpStatusCode(HttpStatusCode.BadRequest)]
    ValidationFailed = 1,           // Returns HTTP 400 (not 1!)

    [HttpStatusCode(HttpStatusCode.NotFound)]
    EntityNotFound = 2,             // Returns HTTP 404 (not 2!)

    [HttpStatusCode(HttpStatusCode.InternalServerError)]
    UnexpectedError = 99            // Returns HTTP 500 (not 99!)
}
```

When using BPITS.Results alone, returning `ApiResult` from a controller would result in HTTP 200 for all responses, with your custom status codes only available in the response body. BPITS.Results.AspNetCore solves this by providing automatic mapping to appropriate HTTP status codes, giving you the best of both worlds: application-specific status codes for internal logic **and** standard HTTP status codes for clients, tooling, and infrastructure.

## Complete Example

```csharp
// 1. Define enum with attributes
[ResultStatusCode(
    IncludeActionResultMapper = true,
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(BadRequest)
)]
public enum MyApiStatusCode
{
    [HttpStatusCode(HttpStatusCode.OK)]
    Ok = 0,

    [HttpStatusCode(HttpStatusCode.BadRequest)]
    BadRequest = 400,

    [HttpStatusCode(HttpStatusCode.Unauthorized)]
    Unauthorized = 401,

    [HttpStatusCode(HttpStatusCode.Forbidden)]
    Forbidden = 403,

    [HttpStatusCode(HttpStatusCode.NotFound)]
    NotFound = 404,

    [HttpStatusCode(HttpStatusCode.InternalServerError)]
    InternalServerError = 500
}

// 2. Register in Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMyApiStatusCodeActionResultMapper();

// 3. Use in services
public class UserService
{
    public ServiceResult<User> GetUser(int id)
    {
        var user = _repository.Find(id);
        if (user == null)
            return ServiceResult.Failure<User>("User not found", MyApiStatusCode.NotFound);

        return user;
    }
}

// 4. Return from controllers
[ApiController]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public ApiResult<UserDto> GetUser(int id)
    {
        var result = _userService.GetUser(id);
        return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
        // Returns 404 with error message if not found
        // Returns 200 with UserDto if successful
    }
}
```

## Architecture

This package provides:
- **`IActionResultMapper<TEnum>`** - Interface for HTTP status code mapping
- **Partial `ApiResult<TEnum>` class** - Implements `IActionResult` when enabled
- **Attribute-based mapper** - Automatically generated based on `[HttpStatusCode]` attributes
- **DI registration extensions** - `AddMyEnumActionResultMapper()` methods for easy setup
- **Source generators** - Creates all implementation code at compile time

## Dependencies

- BPITS.Results (base package)
- Microsoft.AspNetCore.Mvc.Abstractions >= 2.2.0