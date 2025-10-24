# BPITS.Results.AspNetCore

ASP.NET Core extensions for BPITS.Results that enable `ApiResult` to implement `IActionResult` with customizable HTTP status code mapping.

## Features

- **Flexible HTTP Status Code Mapping**: Multiple approaches to define enum→HTTP mappings
- **IActionResult Implementation**: `ApiResult` can be returned directly from ASP.NET Core controllers  
- **Source Generated**: Zero runtime overhead with compile-time code generation
- **Safe Defaults**: Graceful fallbacks when no custom mapping is provided
- **Dependency Injection Integration**: Simple registration with built-in service collection extensions

## Installation

```bash
# Install both packages
dotnet add package BPITS.Results
dotnet add package BPITS.Results.AspNetCore
```

## Quick Start

1. **Define your status enum with ActionResult support**:
```csharp
[ResultStatusCode(includeActionResultMapper: true)]
public enum MyApiStatusCode
{
    GenericFailure = 0,
    Ok = 1,
    BadRequest = 2,
    ResourceNotFound = 3,
    // ... other values
}
```

2. **Choose your mapping approach** (see options below)

3. **Register the mapper in DI** (in `Program.cs` or `Startup.cs`):
```csharp
services.AddMyApiStatusCodeActionResultMapper(); // Uses default mapper
// OR
services.AddSingleton<IActionResultMapper<MyApiStatusCode>, CustomMyApiStatusCodeActionResultMapper>();
```

4. **Use in controllers**:
```csharp
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        var result = _userService.GetUser(id); // Returns ServiceResult<User>
        return ApiResult.FromServiceResult(result).ToActionResult(ControllerContext);
    }
}
```

## HTTP Status Code Mapping Options

### Option 1: Attribute-Based Mapping (Recommended)

Use `[HttpStatusCode]` attributes directly on enum values:

```csharp
[ResultStatusCode(includeActionResultMapper: true)]
public enum MyApiStatusCode
{
    [HttpStatusCode(HttpStatusCode.InternalServerError)]
    GenericFailure = 0,
    
    [HttpStatusCode(HttpStatusCode.OK)]
    Ok = 1,
    
    [HttpStatusCode(HttpStatusCode.BadRequest)]
    BadRequest = 2,
    
    [HttpStatusCode(HttpStatusCode.NotFound)]
    ResourceNotFound = 3
}
```

The source generator automatically creates an `AttributeMyApiStatusCodeActionResultMapper` that uses these mappings.

### Option 2: Custom Implementation

Implement `IActionResultMapper<TEnum>` yourself:

```csharp
public class CustomMyApiStatusCodeActionResultMapper : IActionResultMapper<MyApiStatusCode>
{
    public HttpStatusCode MapStatusCode(MyApiStatusCode statusCode)
    {
        return statusCode switch
        {
            MyApiStatusCode.Ok => HttpStatusCode.OK,
            MyApiStatusCode.BadRequest => HttpStatusCode.BadRequest,
            MyApiStatusCode.ResourceNotFound => HttpStatusCode.NotFound,
            MyApiStatusCode.GenericFailure => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }
}
```

Register your custom implementation:
```csharp
services.AddSingleton<IActionResultMapper<MyApiStatusCode>, CustomMyApiStatusCodeActionResultMapper>();
```

### Option 3: Extend Generated Template

The source generator creates a `CustomMyApiStatusCodeActionResultMapper` template with suggested mappings:

```csharp
public class CustomMyApiStatusCodeActionResultMapper : IActionResultMapper<MyApiStatusCode>
{
    public HttpStatusCode MapStatusCode(MyApiStatusCode statusCode)
    {
        return statusCode switch
        {
            // MyApiStatusCode.Ok => HttpStatusCode.OK, // TODO: Choose appropriate status code
            // MyApiStatusCode.BadRequest => HttpStatusCode.BadRequest, // TODO: Choose appropriate status code
            // MyApiStatusCode.ResourceNotFound => HttpStatusCode.NotFound, // TODO: Choose appropriate status code
            // MyApiStatusCode.GenericFailure => HttpStatusCode.InternalServerError, // TODO: Choose appropriate status code
            _ => HttpStatusCode.InternalServerError
        };
    }
}
```

Uncomment and customize the mappings as needed.

## Default Behavior

If no custom mapper is registered, the system falls back to a safe default:
- `Ok` enum values → `200 OK`
- All other values → `500 Internal Server Error`

This ensures your application works even without explicit mapping configuration.

## Architecture

This package provides:
- `IActionResultMapper<TEnum>` interface for HTTP status code mapping
- `AspNetCoreApiResult<TEnum>` that implements `IActionResult`
- Multiple mapper implementations (default, attribute-based, custom template)
- DI registration extensions for seamless integration
- Source generators that create all the plumbing automatically

## Dependencies

- BPITS.Results (base package)
- Microsoft.AspNetCore.Mvc.Abstractions >= 2.2.0