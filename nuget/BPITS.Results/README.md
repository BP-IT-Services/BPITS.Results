# BPITS.Results

A robust .NET implementation of the Result pattern with source generation for type-safe error handling across service layers and APIs.

## Overview

BPITS.Results provides two complementary result types designed for different layers of your application:

- **`ServiceResult<T>`** - For internal service layer operations with full error context including exceptions
- **`ApiResult<T>`** - For API responses with sanitized error information suitable for public consumption

Both types support source generation, allowing you to define custom status codes with your own enums and automatically generate strongly-typed result types.

## Installation

```bash
dotnet add package BPITS.Results
```

## Quick Start

### 1. Define Your Status Code Enum

```csharp
using BPITS.Results.Abstractions;

[GenerateApiResult]
[GenerateServiceResult]
public enum MyAppStatus
{
    Ok = 0,
    BadRequest = 400,
    ResourceNotFound = 404,
    InternalServerError = 500
}
```

### 2. Use in Your Services

```csharp
public class UserService
{
    public async Task<ServiceResult<User>> GetUserAsync(Guid userId)
    {
        try
        {
            var user = await _repository.FindAsync(userId);
            if (user == null)
                return ServiceResult.Failure<User>("User not found", MyAppStatus.ResourceNotFound);

            return user; // Implicit conversion to ServiceResult.Success(user)
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure<User>(ex, "Failed to retrieve user");
        }
    }
}
```

### 3. Use in Your Controllers

```csharp
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

## Core Features

- **Source-generated Result types** - Define your own status codes with enums
- **Type-safe error handling** - No more exception-driven control flow
- **Validation error support** - Field-level validation errors with detailed messages
- **Safe value extraction** - TryGet pattern eliminates null checking
- **Type transformations** - MapValue for seamless DTO conversions
- **Error propagation** - PassThroughFail for chaining operations
- **Implicit conversions** - Cleaner, more readable code

## Documentation

For comprehensive guides and examples, visit the [complete documentation](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/README.md):

### Getting Started
- [Installation Guide](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/getting-started/installation.md)
- [Quick Start Tutorial](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/getting-started/quick-start.md)
- [Core Concepts](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/getting-started/core-concepts.md) - Understanding ServiceResult vs ApiResult

### Common Patterns
- [Working with Results](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/guides/working-with-results.md) - Creating, extracting, and converting results
- [Validation Patterns](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/guides/validation-patterns.md) - Handling validation errors
- [Controller Patterns](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/guides/controller-patterns.md) - Using results in ASP.NET Core
- [Error Handling](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/guides/error-handling.md) - Managing errors and status codes

### Advanced Topics
- [Custom Status Codes](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/advanced/custom-status-codes.md) - Configuring DefaultFailureValue and BadRequestValue

### Integration
- [Entity Framework](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/integration/entity-framework.md) - Database operations
- [FluentValidation](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/integration/fluentvalidation.md) - Validation library integration
- [Dependency Injection](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/integration/dependency-injection.md) - DI and testing patterns

## ASP.NET Core Integration

For ASP.NET Core with automatic HTTP status code mapping, install the companion package:

```bash
dotnet add package BPITS.Results.AspNetCore
```

See the [ASP.NET Core Integration Guide](https://github.com/BP-IT-Services/BPITS.Results/blob/main/nuget/docs/guides/aspnetcore-integration.md) for setup instructions.

## Repository

[GitHub Repository](https://github.com/BP-IT-Services/BPITS.Results)

## License

MIT License - see the [LICENSE](https://github.com/BP-IT-Services/BPITS.Results/blob/main/LICENSE) file for details.
