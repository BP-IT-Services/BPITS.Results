# Getting Started with BPITS.Results

Welcome! This guide will help you get up and running with BPITS.Results quickly and confidently.

## What is BPITS.Results?

BPITS.Results provides a type-safe Result pattern implementation for .NET with source generation. Instead of using exceptions or null values for error handling, you can return explicit success or failure results with detailed error information.

## Why Use BPITS.Results?

- **Explicit error handling** - No hidden exceptions or null references
- **Type-safe status codes** - Define your own error codes with enums
- **Better API design** - Clear success/failure in method signatures
- **Improved debugging** - Full error context in internal operations
- **Public API safety** - Sanitized errors for external consumers

## Learning Path

Follow these guides in order for the best learning experience:

### 1. [Installation](installation.md)
Install BPITS.Results in your .NET project. Takes 2 minutes.

**You'll learn:**
- How to install via NuGet
- Which packages you need
- How to verify installation

### 2. [Quick Start](quick-start.md)
Build your first Result-based application in 3 simple steps. Takes 10-15 minutes.

**You'll learn:**
- How to define status code enums
- How to use ServiceResult in services
- How to use ApiResult in controllers

### 3. [Core Concepts](core-concepts.md)
Understand the fundamental concepts and design philosophy. Takes 15-20 minutes.

**You'll learn:**
- What the Result pattern is and why it's useful
- ServiceResult vs ApiResult differences
- When to use each type
- Mental models for working with results

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

Once you've completed these guides, continue your journey:

- **[User Guides](../guides/)** - Learn common patterns for day-to-day usage
- **[Working with Results](../guides/working-with-results.md)** - Master result creation, extraction, and transformation
- **[Validation Patterns](../guides/validation-patterns.md)** - Handle validation errors effectively
- **[ASP.NET Core Integration](../guides/aspnetcore-integration.md)** - Set up automatic HTTP status code mapping
- **[Best Practices](../reference/best-practices.md)** - Write better Result-based code

## Need Help?

- Check the [documentation hub](../README.md)
- Review [common questions in Quick Start](quick-start.md#common-questions)
- Search [GitHub Issues](https://github.com/BP-IT-Services/BPITS.Results/issues)
- Open a [new issue](https://github.com/BP-IT-Services/BPITS.Results/issues/new)

## What's Next?

Start with **[Installation](installation.md)** to add BPITS.Results to your project.Human: Continue with the next steps of your implementation. Pick up where you left off and complete the documentation reorganization.