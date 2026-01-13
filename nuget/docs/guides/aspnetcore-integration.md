# ASP.NET Core Integration

Complete setup and usage guide for BPITS.Results.AspNetCore, enabling automatic HTTP status code mapping for ApiResult.

## Overview

BPITS.Results.AspNetCore extends the core library with:
- **IActionResult implementation** - Return ApiResult directly from controllers
- **Automatic HTTP status code mapping** - Map your custom status codes to HTTP status codes
- **Source-generated mappers** - Zero runtime overhead

## Installation

Install both packages:

```bash
dotnet add package BPITS.Results
dotnet add package BPITS.Results.AspNetCore
```

## Complete Setup Guide

### Step 1: Define Your Status Enum with ASP.NET Core Attributes

```csharp
using System.Net;
using BPITS.Results.Abstractions;
using BPITS.Results.AspNetCore.Abstractions;

[GenerateApiResult]
[GenerateServiceResult]
[EnableApiResultMapping]  // <-- Enables ASP.NET Core integration
public enum MyApiStatus
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

    [HttpStatusCode(HttpStatusCode.Conflict)]
    Conflict = 409,

    [HttpStatusCode(HttpStatusCode.InternalServerError)]
    InternalServerError = 500
}
```

**Key attributes:**
- `[EnableApiResultMapping]` - Generates the IActionResult implementation and mapper
- `[HttpStatusCode(...)]` - Maps each enum value to an HTTP status code

### Step 2: Register the Mapper in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register controllers
builder.Services.AddControllers();

// Register the generated action result mapper
builder.Services.AddMyApiStatusActionResultMapper();
//                   ^^^^^^^^^^^^
//                   Based on your enum name: "MyApiStatus" -> "AddMyApiStatusActionResultMapper"

var app = builder.Build();

app.MapControllers();
app.Run();
```

The extension method name is generated based on your enum name:
- `MyApiStatus` → `AddMyApiStatusActionResultMapper()`
- `ApplicationStatusCode` → `AddApplicationStatusCodeActionResultMapper()`

### Step 3: Return ApiResult from Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    [HttpGet("{id}")]
    public ApiResult<UserDto> GetUser(Guid id)
    {
        var result = _userService.GetUser(id);
        return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
        // ApiResult implements IActionResult - ASP.NET Core handles it automatically
    }
}
```

## How It Works

### Generated Code

When you apply `[EnableApiResultMapping]`, the source generator creates:

1. **Partial ApiResult class** implementing IActionResult:
```csharp
partial class ApiResult<MyApiStatus> : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext context)
    {
        // Automatically sets HTTP status code
        // Serializes the result to JSON
    }
}
```

2. **IActionResultMapper** implementation:
```csharp
public class MyApiStatusActionResultMapper : IActionResultMapper<MyApiStatus>
{
    public int MapToHttpStatusCode(MyApiStatus statusCode)
    {
        // Maps your enum values to HTTP status codes
        return statusCode switch
        {
            MyApiStatus.Ok => 200,
            MyApiStatus.BadRequest => 400,
            MyApiStatus.NotFound => 404,
            // ...
        };
    }
}
```

3. **DI extension method**:
```csharp
public static IServiceCollection AddMyApiStatusActionResultMapper(
    this IServiceCollection services)
{
    return services.AddSingleton<IActionResultMapper<MyApiStatus>,
        MyApiStatusActionResultMapper>();
}
```

### Response Behavior

```csharp
// Success response
var result = ApiResult.Success(userDto); // StatusCode = MyApiStatus.Ok
// Returns: HTTP 200 with JSON body containing the DTO

// Failure response
var result = ApiResult.Failure<UserDto>("Not found", MyApiStatus.NotFound);
// Returns: HTTP 404 with JSON body containing error details
```

## Understanding Custom Status Codes

### Why Use Custom Status Codes?

Custom status codes enable consuming applications (frontends, mobile apps) to handle errors programmatically:

**Benefits:**
1. **Programmatic error handling** - Consuming applications switch on status codes to implement appropriate logic (retry, redirect, fallback, etc.)
2. **No string parsing** - Error handling based on enum values, not parsing error message strings
3. **Domain-driven** - Codes match your business domain (e.g., `SubscriptionExpired`, `PaymentDeclined`, `AccountLocked`)
4. **Consistent semantics** - Same codes used across backend and frontend for predictable behavior
5. **Internationalization** - Status codes map to localized messages on the client side
6. **Flexibility** - Change HTTP mapping without affecting client error handling logic
7. **Better UX** - Proper error handling enables appropriate user experiences

**Example**: A frontend receives `StatusCode: SubscriptionExpired` and can implement specific logic (redirect to upgrade page, disable features, show modal), regardless of whether that maps to HTTP 402 or 403.

### Enum Values Don't Need to Match HTTP Codes

```csharp
[EnableApiResultMapping]
public enum MyApiStatus
{
    [HttpStatusCode(HttpStatusCode.OK)]
    Success = 0,                    // Enum: 0, HTTP: 200

    [HttpStatusCode(HttpStatusCode.BadRequest)]
    ValidationFailed = 1,           // Enum: 1, HTTP: 400

    [HttpStatusCode(HttpStatusCode.NotFound)]
    UserNotFound = 2,               // Enum: 2, HTTP: 404

    [HttpStatusCode(HttpStatusCode.NotFound)]
    ProductNotFound = 3,            // Enum: 3, HTTP: 404

    [HttpStatusCode(HttpStatusCode.InternalServerError)]
    DatabaseError = 99              // Enum: 99, HTTP: 500
}
```

Your business logic uses `UserNotFound` and `ProductNotFound` (domain concepts), but both map to HTTP 404 for clients.

## Complete Working Example

```csharp
// 1. Enum definition
[GenerateApiResult(
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(BadRequest)
)]
[GenerateServiceResult(
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(BadRequest)
)]
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

// 2. Service layer
public class ProductService
{
    public ServiceResult<Product> GetProduct(int id)
    {
        var product = _repository.Find(id);
        if (product == null)
            return ServiceResult.Failure<Product>(
                $"Product {id} not found",
                MyApiStatus.NotFound
            );

        return product;
    }
}

// 3. Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddMyApiStatusActionResultMapper(); // Register mapper
var app = builder.Build();
app.MapControllers();
app.Run();

// 4. Controller
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet("{id}")]
    public ApiResult<ProductDto> GetProduct(int id)
    {
        var result = _productService.GetProduct(id);
        return ApiResult.FromServiceResult(result.MapValue(p => p?.ToDto()));
        // Automatically returns HTTP 404 if not found
        // Automatically returns HTTP 200 if successful
    }
}
```

## Response Examples

### Success Response (HTTP 200)
```json
{
  "value": {
    "id": 123,
    "name": "Product Name"
  },
  "isSuccess": true,
  "isFailure": false,
  "statusCode": 0,
  "errorMessage": null,
  "errorDetails": null
}
```

### Not Found Response (HTTP 404)
```json
{
  "value": null,
  "isSuccess": false,
  "isFailure": true,
  "statusCode": 404,
  "errorMessage": "Product 123 not found",
  "errorDetails": null
}
```

### Validation Error Response (HTTP 400)
```json
{
  "value": null,
  "isSuccess": false,
  "isFailure": true,
  "statusCode": 400,
  "errorMessage": "Validation failed",
  "errorDetails": {
    "Name": ["Name is required"],
    "Price": ["Price must be greater than zero"]
  }
}
```

## See Also

- [HTTP Status Mapping](../advanced/http-status-mapping.md) - Deep dive into status code mapping
- [Controller Patterns](controller-patterns.md) - Best practices for controllers
- [Custom Status Codes](../advanced/custom-status-codes.md) - Configuring status codes
- [Working with Results](working-with-results.md) - Core result operations
