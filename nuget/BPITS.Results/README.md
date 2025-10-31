# BPITS.Results

A robust .NET implementation of the Result pattern with source generation for type-safe error handling across service layers and APIs.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Quick Start](#quick-start)
    - [1. Define Your Status Code Enum](#1-define-your-status-code-enum)
    - [2. Use in Your Services](#2-use-in-your-services)
    - [3. Use in Your Controllers](#3-use-in-your-controllers)
- [Core Concepts](#core-concepts)
    - [ServiceResult vs ApiResult](#serviceresult-vs-apiresult)
    - [When to Use Each](#when-to-use-each)
- [Working with Results](#working-with-results)
    - [Creating Results](#creating-results)
    - [Extracting Values Safely](#extracting-values-safely)
    - [Type Conversion with MapValue](#type-conversion-with-mapvalue)
    - [Converting Between Result Types](#converting-between-result-types)
- [Advanced Patterns](#advanced-patterns)
    - [Error Propagation](#error-propagation)
    - [Validation Patterns](#validation-patterns)
    - [Controller Patterns](#controller-patterns)
- [Error Details and Status Codes](#error-details-and-status-codes)
    - [Working with Error Details](#working-with-error-details)
    - [Custom Status Code Configuration](#custom-status-code-configuration)
- [Best Practices](#best-practices)
- [Integration Examples](#integration-examples)
    - [With Entity Framework](#with-entity-framework)
    - [With FluentValidation](#with-fluentvalidation)

## Overview

BPITS.Results provides two complementary result types designed for different layers of your application:

- **`ServiceResult<T>`** - For internal service layer operations with full error context including exceptions
- **`ApiResult<T>`** - For API responses with sanitized error information suitable for public consumption

Both types support generic and non-generic variants, offering flexibility for operations that return values or simply indicate success/failure.

## Installation

```bash
dotnet add package BPITS.Results
```

## Quick Start

### 1. Define Your Status Code Enum

Create an enum with the `GenerateApiResult` and/or `GenerateServiceResult` attributes. The enum **must** include an `Ok` value:

```csharp
using BPITS.Results.Abstractions;

// Generate both ApiResult and ServiceResult for this enum
[GenerateApiResult]
[GenerateServiceResult]
public enum MyAppStatus
{
    Ok = 0,
    BadRequest = 400,
    ResourceNotFound = 404,
    GenericFailure = 500
}

// Or generate only one type if needed
[GenerateServiceResult]  // Only generates ServiceResult
public enum InternalStatus
{
    Ok = 0,
    DatabaseError = 500
}
```

You can also configure which enum values to use for default failures and validation errors:

```csharp
[GenerateApiResult(
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(ValidationError)
)]
[GenerateServiceResult(
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(ValidationError)
)]
public enum MyAppStatus
{
    Ok = 0,
    ValidationError = 400,
    Unauthorized = 401,
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
            
            return user; // Return using implicit cast to ServiceResult.Success(user)
            
            // Alternatively, we could return with the more explicit syntax using:
            // return ServiceResult.Success(user); 
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
        
        // Convert ServiceResult to ApiResult and map to DTO
        // Here we return using an implicit cast to ApiResult.FromServiceResult(result.MapValue(user => user?.ToDto())); 
        result.MapValue(user => user?.ToDto());
        
        // Alternative, we could return with the more explicit syntax using:
        // return ApiResult.FromServiceResult(result.MapValue(user => user?.ToDto()));
    }
}
```

## Core Concepts

### ServiceResult vs ApiResult

| Aspect | ServiceResult | ApiResult |
|--------|---------------|-----------|
| **Purpose** | Internal service operations | Public API responses |
| **Exception Details** | ✅ Includes full exception info | ❌ Excludes exception details |
| **Error Context** | ✅ Rich error information | ✅ Sanitized error messages |
| **Use Cases** | Service layer, business logic | Controllers, API endpoints |
| **Security** | Internal debugging | Public-safe error information |

### When to Use Each

**Use `ServiceResult<T>` when:**
- Implementing business logic in services
- Need full exception context for debugging
- Handling internal operations
- Chaining multiple service calls

**Use `ApiResult<T>` when:**
- Returning responses from controllers
- Exposing data through APIs
- Need to hide internal implementation details
- Converting from service results for public consumption

## Working with Results

### Creating Results

#### Success Results

```csharp
// Generic success with value
var userResult = ServiceResult.Success(user);
var apiUserResult = ApiResult.Success(userDto);

// Non-generic success (no value)
var operationResult = ServiceResult.Success();
var apiOperationResult = ApiResult.Success();

// Implicit conversion from value
ServiceResult<User> result = user; // Automatically creates Success result
ApiResult<UserDto> apiResult = userDto;
```

#### Failure Results

```csharp
// Basic failure
var result = ServiceResult.Failure<User>("User not found", MyAppStatus.ResourceNotFound);

// Failure with exception
var result = ServiceResult.Failure<User>(exception, "Database error", MyAppStatus.GenericFailure);

// Validation failure with field details
var result = ServiceResult.ValidationFailure<User>("Email", "Email address is required");

// Validation failure with multiple field details
var result = ServiceResult.ValidationFailure<User>(new Dictionary<string, string>() 
{
    { "Email", ["Email address is required.", "Email address must match."]},
    { "Password", ["Password is required."]}
});
```

### Extracting Values Safely

The `TryGet` method is the recommended way to extract values, as it eliminates null checks:

```csharp
public async Task<ApiResult<UserDto>> GetUser(Guid id)
{
    var serviceResult = await _userService.GetUserAsync(id);
    
    // Safe value extraction with TryGet
    if (serviceResult.TryGet(out var user))
    {
        // user is guaranteed to be non-null here
        return user.ToDto(); // Return using implicit cast
    }
    
    // Handle failure case
    return ApiResult.FromServiceResult(serviceResult.MapValue<UserDto>(_ => null)); // Return using explicit syntax
}
```

Alternative approaches:

```csharp
// Direct property access (requires null checking)
if (result.IsSuccess && result.Value != null)
{
    var user = result.Value;
    // ... work with user
}

// Get() method (throws if null)
try 
{
    var user = result.Get(); // Throws ArgumentNullException if Value is null
}
catch (ArgumentNullException)
{
    // Handle null value
}
```

### Type Conversion with MapValue

`MapValue` allows you to transform the result's value type while preserving the status and error information:

```csharp
// Convert entity to DTO
var userResult = await _userService.GetUserAsync(id);
var userDtoResult = userResult.MapValue(user => user?.ToDto());

// Handle null values explicitly
var result = serviceResult.MapValueWhenNotNull(user => user.ToDto());

// Complex mapping with different functions for null/non-null
var result = serviceResult.MapValue(
    whenValueNotNullFunc: user => user.ToDetailedDto(),
    whenValueNullFunc: _ => new UserDto { Name = "Unknown" }
);

// Chain multiple transformations
var finalResult = serviceResult
    .MapValue(user => user?.ToDto())
    .MapValue(dto => dto?.ToApiModel());
```

### Converting Between Result Types

#### ServiceResult to ApiResult

```csharp
// Explicit conversion
var serviceResult = await _userService.GetUserAsync(id);
var apiResult = ApiResult.FromServiceResult(serviceResult);

// Implicit conversion
ServiceResult<User> serviceResult = await _userService.GetUserAsync(id);
ApiResult<User> apiResult = serviceResult; // Automatic conversion

// Convert with type mapping
var apiResult = ApiResult.FromServiceResult(
    serviceResult.MapValue(user => user?.ToDto())
);

// Override error message or status code
var apiResult = ApiResult.FromServiceResult(
    serviceResult,
    errorMessage: "Custom error message",
    statusCode: MyAppStatus.BadRequest
);
```

#### Chaining Operations

```csharp
public async Task<ServiceResult<OrderDto>> CreateOrderAsync(CreateOrderRequest request)
{
    // Validate user
    var userResult = await _userService.GetUserAsync(request.UserId);
    if (!userResult.TryGet(out var user))
        return userResult.PassThroughFail<OrderDto>();
    
    // Validate products
    var productsResult = await _productService.GetProductsAsync(request.ProductIds);
    if (!productsResult.TryGet(out var products))
        return productsResult.PassThroughFail<OrderDto>();
    
    // Create order
    var order = new Order(user, products);
    var createResult = await _orderRepository.CreateAsync(order);
    
    return createResult.MapValue(o => o?.ToDto());
}
```

## Advanced Patterns

### Error Propagation

Use `PassThroughFail` to propagate errors while changing the result type:

```csharp
public async Task<ServiceResult<ProcessedData>> ProcessUserDataAsync(Guid userId)
{
    var userResult = await GetUserAsync(userId);
    if (!userResult.TryGet(out var user))
    {
        // Propagate the failure but change the return type
        return userResult.PassThroughFail<ProcessedData>();
    }
    
    // Continue with processing...
    return ProcessData(user);
}
```

### Validation Patterns

```csharp
public async Task<ServiceResult<User>> CreateUserAsync(CreateUserRequest request)
{
    // Basic validation
    if (string.IsNullOrEmpty(request.Email))
    {
        return ServiceResult.ValidationFailure<User>(
            nameof(request.Email), 
            "Email is required"
        );
    }
    
    // Complex validation with multiple errors
    var validationErrors = new Dictionary<string, string[]>();
    
    if (string.IsNullOrEmpty(request.Email))
        validationErrors[nameof(request.Email)] = new[] { "Email is required" };
        
    if (string.IsNullOrEmpty(request.Name))
        validationErrors[nameof(request.Name)] = new[] { "Name is required" };
    
    if (validationErrors.Any())
    {
        return ServiceResult.ValidationFailure<User>(
            "Validation failed", 
            validationErrors
        );
    }
    
    // Continue with creation...
}
```

### Controller Patterns

#### Basic Controller Action

```csharp
[HttpGet("{id}")]
public async Task<ApiResult<UserDto>> GetUser(Guid id)
{
    var result = await _userService.GetUserAsync(id);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}
```

#### Handling Paged Results

```csharp
[HttpGet]
public async Task<ApiResult<PagedResult<UserDto>>> GetUsersPaged(
    [FromQuery] PageParams pageParams)
{
    var result = await _userService.GetUsersPagedAsync(pageParams);
    
    if (result.TryGet(out var pagedResult))
    {
        // Transform each item in the paged result
        return pagedResult.Select(user => user.ToDto());
    }
    
    return ApiResult.FromServiceResult(result.MapValue<PagedResult<UserDto>>(_ => null));
}
```

#### Exception Handling in Controllers

```csharp
[HttpPost]
public async Task<ApiResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
{
    try
    {
        var result = await _userService.CreateUserAsync(request);
        return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create user");
        return ApiResult.Failure<UserDto>(
            "An error occurred while creating the user", 
            MyAppStatus.GenericFailure
        );
    }
}
```

## Error Details and Status Codes

### Working with Error Details

```csharp
// Check for specific error conditions
if (result.IsFailure)
{
    switch (result.StatusCode)
    {
        case MyAppStatus.ResourceNotFound:
            // Handle not found
            break;
        case MyAppStatus.BadRequest:
            // Handle validation errors
            if (result.ErrorDetails?.Any() == true)
            {
                foreach (var error in result.ErrorDetails)
                {
                    Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value)}");
                }
            }
            break;
    }
}
```

### Custom Status Code Configuration

The `GenerateApiResult` and `GenerateServiceResult` attributes allow you to customize which enum values are used for common failure scenarios:

#### DefaultFailureValue

Controls which status code is used when creating failures without explicitly specifying a status code:

```csharp
[GenerateApiResult(DefaultFailureValue = nameof(InternalServerError))]
[GenerateServiceResult(DefaultFailureValue = nameof(InternalServerError))]
public enum MyAppStatus
{
    Ok = 0,
    BadRequest = 400,
    InternalServerError = 500
}

// This will use InternalServerError instead of the enum's default value (0/Ok)
var result = ServiceResult.Failure<User>("Something went wrong");
// result.StatusCode will be MyAppStatus.InternalServerError
```

**Without DefaultFailureValue specified:**
```csharp
var result = ServiceResult.Failure<User>("Something went wrong");
// result.StatusCode will be the default enum value (typically 0)
```

#### BadRequestValue

Controls which status code is used for validation failures:

```csharp
[GenerateApiResult(BadRequestValue = nameof(ValidationError))]
[GenerateServiceResult(BadRequestValue = nameof(ValidationError))]
public enum MyAppStatus
{
    Ok = 0,
    ValidationError = 400,
    InternalServerError = 500
}

// These validation methods will use ValidationError
var result1 = ServiceResult.ValidationFailure<User>("Invalid email");
var result2 = ServiceResult.ValidationFailure<User>("Email", "Email is required");
// Both results will have StatusCode = MyAppStatus.ValidationError
```

**Without BadRequestValue specified:**
```csharp
// The generator looks for a "BadRequest" enum value
public enum MyAppStatus
{
    Ok = 0,
    BadRequest = 400,  // This will be used automatically
    InternalServerError = 500
}

// If no "BadRequest" value exists, it falls back to the default enum value
```

#### Complete Configuration Example

```csharp
[GenerateApiResult(
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(ValidationFailed)
)]
[GenerateServiceResult(
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(ValidationFailed)
)]
public enum ApplicationStatusCode
{
    Ok = 0,
    ValidationFailed = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    InternalServerError = 500,
    ServiceUnavailable = 503
}
```

#### Impact on Generated Methods

The configuration affects these methods:

```csharp
// Uses DefaultFailureValue
ServiceResult.Failure<T>("error message");
ServiceResult.Failure("error message");

// Uses BadRequestValue  
ServiceResult.ValidationFailure<T>("error message");
ServiceResult.ValidationFailure<T>("field", "error");
ServiceResult.ValidationFailure("error message");
ServiceResult.ValidationFailure("field", "error");
```

#### Fallback Behavior

1. **For DefaultFailureValue**: If not specified, uses the enum's default value (typically the first enum member or value 0)
2. **For BadRequestValue**: If not specified, the generator looks for an enum member named "BadRequest". If not found, falls back to the enum's default value

## Best Practices

### 1. Consistent Error Handling

```csharp
// Good: Consistent pattern
public async Task<ServiceResult<T>> GetEntityAsync<T>(Guid id) where T : class
{
    try
    {
        var entity = await _repository.FindAsync<T>(id);
        if (entity == null)
            return ServiceResult.Failure<T>("Entity not found", MyAppStatus.ResourceNotFound);
            
        return ServiceResult.Success(entity);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve entity {EntityType} with ID {Id}", typeof(T).Name, id);
        return ServiceResult.Failure<T>(ex, "Failed to retrieve entity");
    }
}
```

### 2. Prefer TryGet Over Direct Property Access

```csharp
// Good: Safe value extraction
if (result.TryGet(out var user))
{
    // user is guaranteed non-null
    ProcessUser(user);
}

// Avoid: Requires null checking
if (result.IsSuccess && result.Value != null)
{
    ProcessUser(result.Value);
}
```

### 3. Use MapValue for Type Transformations

```csharp
// Good: Clear transformation chain
var apiResult = serviceResult
    .MapValue(entity => entity?.ToDto())
    .MapValue(dto => dto?.ToApiModel());

// Avoid: Manual null checking and conversion
ApiResult<ApiModel> apiResult;
if (serviceResult.IsSuccess && serviceResult.Value != null)
{
    var dto = serviceResult.Value.ToDto();
    if (dto != null)
    {
        apiResult = ApiResult.Success(dto.ToApiModel());
    }
    else
    {
        apiResult = ApiResult.Failure<ApiModel>("Conversion failed");
    }
}
else
{
    apiResult = ApiResult.FromServiceResult(serviceResult.MapValue<ApiModel>(_ => null));
}
```

### 4. Meaningful Error Messages

```csharp
// Good: Specific, actionable error messages
return ServiceResult.Failure<User>(
    "User with email 'john@example.com' already exists", 
    MyAppStatus.BadRequest
);

// Avoid: Generic, unhelpful messages
return ServiceResult.Failure<User>("Error", MyAppStatus.GenericFailure);
```

## Integration Examples

### With Entity Framework

```csharp
public class UserRepository
{
    public async Task<ServiceResult<User>> CreateAsync(User user)
    {
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return ServiceResult.Success(user);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint") == true)
        {
            return ServiceResult.ValidationFailure<User>("Email address is already in use");
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure<User>(ex, "Failed to create user");
        }
    }
}
```

### With FluentValidation

```csharp
public async Task<ServiceResult<User>> CreateUserAsync(CreateUserRequest request)
{
    var validationResult = await _validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            
        return ServiceResult.ValidationFailure<User>("Validation failed", errors);
    }
    
    // Continue with creation...
}
```
