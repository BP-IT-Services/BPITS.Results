# BPITS.Results

A robust .NET implementation of the Result pattern with source generation for type-safe error handling across service layers and APIs.

## Overview

BPITS.Results provides two complementary result types designed for different layers of your application:

- **`ServiceResult<T>`** - For internal service layer operations with full error context including exceptions
- **`ApiResult<T>`** - For API responses with sanitized error information suitable for public consumption

Both types support source generation, allowing you to define custom status codes with your own enums and automatically generate strongly-typed result types for your application.

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| **[BPITS.Results](nuget/BPITS.Results)** | Core result types with source generation | [![NuGet](https://img.shields.io/nuget/v/BPITS.Results.svg)](https://www.nuget.org/packages/BPITS.Results/) |
| **[BPITS.Results.AspNetCore](nuget/BPITS.Results.AspNetCore)** | ASP.NET Core integration with IActionResult support | [![NuGet](https://img.shields.io/nuget/v/BPITS.Results.AspNetCore.svg)](https://www.nuget.org/packages/BPITS.Results.AspNetCore/) |
| BPITS.Results.Abstractions | Core abstractions (included automatically) | [![NuGet](https://img.shields.io/nuget/v/BPITS.Results.Abstractions.svg)](https://www.nuget.org/packages/BPITS.Results.Abstractions/) |
| BPITS.Results.AspNetCore.Abstractions | ASP.NET Core abstractions (included automatically) | [![NuGet](https://img.shields.io/nuget/v/BPITS.Results.AspNetCore.Abstractions.svg)](https://www.nuget.org/packages/BPITS.Results.AspNetCore.Abstractions/) |

## Quick Start

```csharp
using BPITS.Results.Abstractions;

// 1. Define your status code enum with source generation attributes
[GenerateApiResult]
[GenerateServiceResult]
public enum MyAppStatus
{
    Ok = 0,
    BadRequest = 400,
    NotFound = 404,
    InternalServerError = 500
}

// 2. Use in your services
public class UserService
{
    public async Task<ServiceResult<User>> GetUserAsync(Guid userId)
    {
        var user = await _repository.FindAsync(userId);
        if (user == null)
            return ServiceResult.Failure<User>("User not found", MyAppStatus.NotFound);

        return user; // Implicit conversion to ServiceResult.Success
    }
}

// 3. Return from controllers
[ApiController]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ApiResult<UserDto>> GetUser(Guid id)
    {
        var result = await _userService.GetUserAsync(id);
        return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
    }
}
```

See the [Quick Start Guide](nuget/docs/getting-started/quick-start.md) for detailed instructions.

## Features

- **Source-generated Result types** - Define your own status codes with enums
- **Type-safe error handling** - No more exception-driven control flow
- **Validation error support** - Field-level validation errors with detailed messages
- **Safe value extraction** - TryGet pattern eliminates null checking
- **Type transformations** - MapValue for seamless DTO conversions
- **Error propagation** - PassThroughFail for chaining operations
- **ASP.NET Core integration** - Direct IActionResult implementation with automatic HTTP status code mapping
- **Zero runtime overhead** - All code generated at compile time

## Documentation

Comprehensive documentation organized by experience level:

- **[Getting Started](nuget/docs/getting-started/)** - Installation, quick start, and core concepts
- **[User Guides](nuget/docs/guides/)** - Common patterns and day-to-day usage
- **[Advanced Topics](nuget/docs/advanced/)** - In-depth techniques and complex scenarios
- **[Integration Examples](nuget/docs/integration/)** - Entity Framework, FluentValidation, and more
- **[API Reference](nuget/docs/reference/)** - Complete API documentation and best practices

### Quick Links

- [Installation Guide](nuget/docs/getting-started/installation.md)
- [Core Concepts](nuget/docs/getting-started/core-concepts.md) - Understanding ServiceResult vs ApiResult
- [Working with Results](nuget/docs/guides/working-with-results.md) - Creating, extracting, and converting results
- [ASP.NET Core Integration](nuget/docs/guides/aspnetcore-integration.md) - Complete setup guide
- [Best Practices](nuget/docs/reference/best-practices.md)

## Why BPITS.Results?

### Clear Error Handling

Replace exception-driven control flow with explicit success/failure states:

```csharp
// Before: Exception-driven
public async Task<User> GetUserAsync(Guid id)
{
    var user = await _repository.FindAsync(id);
    if (user == null)
        throw new NotFoundException("User not found"); // Exception for control flow

    return user;
}

// After: Explicit result handling
public async Task<ServiceResult<User>> GetUserAsync(Guid id)
{
    var user = await _repository.FindAsync(id);
    if (user == null)
        return ServiceResult.Failure<User>("User not found", MyAppStatus.NotFound);

    return user;
}
```

### Custom Status Codes for Application Error Handling

Custom status codes enable consuming applications to handle different error types programmatically:

```csharp
[GenerateApiResult]
public enum MyAppStatus
{
    Ok = 0,
    UserNotFound = 1,
    InvalidCredentials = 2,
    AccountLocked = 3,
    InternalServerError = 500
}

// Consuming applications can switch on status codes to handle errors appropriately:
// - UserNotFound → Retry with different ID or show search UI
// - InvalidCredentials → Allow retry, track attempts, trigger lockout logic
// - AccountLocked → Redirect to account recovery flow
// - InternalServerError → Retry with exponential backoff, log to error tracking
```

This enables consuming applications to:
- Handle different error types programmatically without parsing error messages
- Implement appropriate retry logic, redirects, or fallback behavior per error type
- Maintain consistent error handling semantics across the entire application
- Support internationalization (status code → localized message mapping)
- Provide better user experiences as a result of proper error handling

### Separation of Concerns

ServiceResult for internal operations, ApiResult for public APIs:

```csharp
// Service layer: Full error context including exceptions
public ServiceResult<Order> ProcessOrder(Order order)
{
    try
    {
        // ... process order
        return order;
    }
    catch (Exception ex)
    {
        return ServiceResult.Failure<Order>(ex, "Failed to process order");
    }
}

// Controller: Sanitized errors without exception details
public ApiResult<OrderDto> CreateOrder(CreateOrderRequest request)
{
    var result = _orderService.ProcessOrder(order);
    return ApiResult.FromServiceResult(result.MapValue(o => o?.ToDto()));
    // Exception details are NOT exposed to the client
}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- [Documentation](nuget/docs/)
- [GitHub Issues](https://github.com/BP-IT-Services/BPITS.Results/issues)
- [GitHub Repository](https://github.com/BP-IT-Services/BPITS.Results)
