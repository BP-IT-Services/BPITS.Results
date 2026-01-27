# Error Handling

Status codes, error details, and error handling strategies for BPITS.Results.

## Overview

BP ITS.Results provides structured error handling through:
- Status codes (custom enums)
- Error messages (human-readable descriptions)
- Error details (field-level validation errors)
- Exception capture (ServiceResult only)

## Working with Error Details

### Accessing Error Information

```csharp
var result = await _userService.GetUserAsync(id);

if (result.IsFailure)
{
    // Status code
    Console.WriteLine($"Status: {result.StatusCode}");

    // Error message
    Console.WriteLine($"Error: {result.ErrorMessage}");

    // Error details (for validation failures)
    if (result.ErrorDetails != null)
    {
        foreach (var (field, errors) in result.ErrorDetails)
        {
            Console.WriteLine($"{field}: {string.Join(", ", errors)}");
        }
    }

    // Exception (ServiceResult only)
    if (result is ServiceResult<User> serviceResult && serviceResult.Exception != null)
    {
        Console.WriteLine($"Exception: {serviceResult.Exception.Message}");
    }
}
```

### Checking for Specific Errors

```csharp
var result = await _userService.GetUserAsync(id);

switch (result.StatusCode)
{
    case MyAppStatus.NotFound:
        _logger.LogWarning("User {UserId} not found", id);
        return ApiResult.FromServiceResult(result.MapValue<UserDto>(_ => null));

    case MyAppStatus.BadRequest:
        _logger.LogWarning("Invalid request for user {UserId}: {Error}",
            id, result.ErrorMessage);
        return ApiResult.FromServiceResult(result.MapValue<UserDto>(_ => null));

    case MyAppStatus.InternalServerError:
        _logger.LogError("Error retrieving user {UserId}: {Error}",
            id, result.ErrorMessage);
        return ApiResult.FromServiceResult(result.MapValue<UserDto>(_ => null));

    default:
        return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}
```

## Status Code Selection

### Guidelines for Status Codes

```csharp
[GenerateApiResult]
[GenerateServiceResult]
public enum MyAppStatus
{
    // Success
    Ok = 0,

    // Client Errors (400-499)
    BadRequest = 400,           // General validation errors
    Unauthorized = 401,         // Authentication required
    Forbidden = 403,            // Not authorized
    NotFound = 404,             // Resource doesn't exist
    Conflict = 409,             // Resource already exists

    // Server Errors (500-599)
    InternalServerError = 500,  // Unexpected errors
    ServiceUnavailable = 503    // Temporary unavailability
}
```

**When to use each:**
- **BadRequest (400)**: Validation errors, invalid input
- **Unauthorized (401)**: User not authenticated
- **Forbidden (403)**: User authenticated but not authorized
- **NotFound (404)**: Resource doesn't exist
- **Conflict (409)**: Resource already exists, duplicate key
- **InternalServerError (500)**: Unexpected exceptions, database errors
- **ServiceUnavailable (503)**: External service down, maintenance mode

### Custom Domain-Specific Codes

```csharp
public enum OrderStatus
{
    Ok = 0,
    InvalidQuantity = 1,
    InsufficientInventory = 2,
    PaymentFailed = 3,
    ShippingUnavailable = 4
}
```

Map to HTTP status codes using `[HttpStatusCode]` attribute (see [ASP.NET Core Integration](aspnetcore-integration.md)).

## Error Message Best Practices

### Good Error Messages

```csharp
// Good: Specific and actionable
"User with email 'john@example.com' already exists"
"Password must be at least 8 characters and contain a number"
"Product ID 12345 not found in inventory"

// Bad: Vague and unhelpful
"Error"
"Invalid input"
"Operation failed"
```

### User-Friendly vs Developer-Friendly

```csharp
// Service layer: Developer-friendly (internal)
return ServiceResult.Failure<Order>(
    ex,
    "Failed to insert order: UNIQUE constraint violation on OrderNumber column",
    MyAppStatus.InternalServerError
);

// Controller: User-friendly (public)
return ApiResult.FromServiceResult(
    serviceResult,
    errorMessage: "An order with this order number already exists"
);
```

## Logging Errors

### Effective Error Logging

```csharp
public class UserService
{
    private readonly ILogger<UserService> _logger;

    public async Task<ServiceResult<User>> GetUserAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving user {UserId}", id);

            var user = await _repository.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", id);
                return ServiceResult.Failure<User>(
                    "User not found",
                    MyAppStatus.NotFound
                );
            }

            _logger.LogInformation("Successfully retrieved user {UserId}", id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return ServiceResult.Failure<User>(ex, "Failed to retrieve user");
        }
    }
}
```

### Logging Levels

- **Information**: Successful operations
- **Warning**: Expected errors (not found, validation failures)
- **Error**: Unexpected exceptions, critical failures

## Exception Handling Patterns

### Try-Catch in Services

```csharp
public async Task<ServiceResult<User>> CreateUserAsync(CreateUserRequest request)
{
    try
    {
        var user = new User { Email = request.Email };
        await _repository.AddAsync(user);
        return user;
    }
    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
    {
        // Specific exception handling
        return ServiceResult.ValidationFailure<User>(
            "Email address is already in use"
        );
    }
    catch (Exception ex)
    {
        // General exception handling
        _logger.LogError(ex, "Failed to create user");
        return ServiceResult.Failure<User>(ex, "Failed to create user");
    }
}
```

## Related

- [Validation Patterns](validation-patterns.md) - Field-level validation errors
- [Working with Results](working-with-results.md) - Extracting error information
- [Custom Status Codes](../advanced/custom-status-codes.md) - Configuring status codes
