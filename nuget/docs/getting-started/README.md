# Getting Started with BPITS.Results

## What is BPITS.Results?

BPITS.Results provides a type-safe Result pattern implementation for .NET with source generation. Instead of using exceptions or null values for error handling, you can return explicit success or failure results with detailed error information.

## Why Use BPITS.Results?

- **Explicit error handling** - No hidden exceptions or null references
- **Type-safe status codes** - Define your own error codes with enums
- **Better API design** - Clear success/failure in method signatures
- **Improved debugging** - Full error context in internal operations
- **Public API safety** - Sanitized errors for external consumers

## Learning Path

### 1. [Installation](installation.md)
Install BPITS.Results in your .NET project.

### 2. [Quick Start](quick-start.md)
Build your first Result-based application in 3 steps.

### 3. [Core Concepts](core-concepts.md)
Understand the fundamental concepts and design philosophy.

## Quick Example

Here's a taste of what BPITS.Results looks like:

```csharp
// Define your status codes
[GenerateApiResult]
[GenerateServiceResult]
public enum MyAppStatus
{
    Ok = 0,
    NotFound = 404,
    InternalServerError = 500
}

// Service layer
public async Task<ServiceResult<User>> GetUserAsync(Guid id)
{
    var user = await _repository.FindAsync(id);
    if (user == null)
        return ServiceResult.Failure<User>("User not found", MyAppStatus.NotFound);

    return user; // Implicit conversion
}

// Controller layer
[HttpGet("{id}")]
public async Task<ApiResult<UserDto>> GetUser(Guid id)
{
    var result = await _userService.GetUserAsync(id);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}
```

## Choose Your Path

- **New to Result Pattern?** Start with [Core Concepts](core-concepts.md) to understand the fundamentals
- **Ready to code?** Jump straight to [Quick Start](quick-start.md)
- **Need to install first?** Begin with [Installation](installation.md)

## After Getting Started

- **[User Guides](../guides/)** - Common patterns for day-to-day usage
- **[Working with Results](../guides/working-with-results.md)** - Result creation, extraction, and transformation
- **[Validation Patterns](../guides/validation-patterns.md)** - Handle validation errors effectively
- **[ASP.NET Core Integration](../guides/aspnetcore-integration.md)** - Automatic HTTP status code mapping
