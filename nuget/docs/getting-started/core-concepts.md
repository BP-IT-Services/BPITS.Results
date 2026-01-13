# Core Concepts

Understanding the fundamental concepts of BPITS.Results will help you use the library effectively.

## What is the Result Pattern?

The Result pattern is an alternative to exception-based error handling. Instead of throwing exceptions for expected error cases, methods return an explicit `Result` type that represents either success or failure.

### Traditional Approach (Exceptions)

```csharp
public async Task<User> GetUserAsync(Guid userId)
{
    var user = await _repository.FindAsync(userId);
    if (user == null)
        throw new NotFoundException("User not found"); // Exception for control flow

    return user;
}

// Caller must use try-catch
try
{
    var user = await _userService.GetUserAsync(id);
    // Work with user
}
catch (NotFoundException ex)
{
    // Handle not found
}
```

###Problems with this approach:
- Exceptions are expensive (performance overhead)
- Unclear which exceptions a method might throw
- Forces try-catch blocks everywhere
- Exceptions should be for exceptional circumstances, not control flow

### Result Pattern Approach

```csharp
public async Task<ServiceResult<User>> GetUserAsync(Guid userId)
{
    var user = await _repository.FindAsync(userId);
    if (user == null)
        return ServiceResult.Failure<User>("User not found", MyAppStatus.NotFound);

    return user;
}

// Caller uses explicit result handling
var result = await _userService.GetUserAsync(id);
if (result.TryGet(out var user))
{
    // Work with user
}
else
{
    // Handle failure
    Console.WriteLine(result.ErrorMessage);
}
```

### Benefits:
- Explicit success/failure in method signature
- No performance overhead of exceptions
- Clear, readable error handling
- Compiler helps ensure error cases are handled

## ServiceResult vs ApiResult

BPITS.Results provides two complementary result types for different layers of your application.

### ServiceResult<T>

**Purpose**: Internal service layer operations with full error context

**Use in**:
- Service classes
- Business logic layer
- Repository implementations
- Internal operations

**Characteristics**:
- Includes exception details for debugging
- Full error context with stack traces
- Rich error information
- Not suitable for public APIs (too much information)

**Example**:
```csharp
public class OrderService
{
    public async Task<ServiceResult<Order>> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            // Business logic...
            return newOrder;
        }
        catch (Exception ex)
        {
            // Exception details are captured
            return ServiceResult.Failure<Order>(ex, "Failed to create order");
        }
    }
}
```

### ApiResult<T>

**Purpose**: Public API responses with sanitized error information

**Use in**:
- Controller actions
- API endpoints
- Public-facing interfaces
- External consumers

**Characteristics**:
- Excludes exception details (security)
- Sanitized error messages
- Suitable for public consumption
- Can be converted from ServiceResult

**Example**:
```csharp
[HttpPost]
public async Task<ApiResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request)
{
    var serviceResult = await _orderService.CreateOrderAsync(request);

    // Convert to ApiResult (exception details are removed)
    return ApiResult.FromServiceResult(serviceResult.MapValue(o => o?.ToDto()));
}
```

### Comparison Table

| Aspect | ServiceResult | ApiResult |
|--------|---------------|-----------|
| **Purpose** | Internal operations | Public API responses |
| **Exception Details** | ✅ Included | ❌ Excluded |
| **Error Context** | ✅ Rich, detailed | ✅ Sanitized |
| **Stack Traces** | ✅ Captured | ❌ Not included |
| **Use Cases** | Services, business logic | Controllers, API endpoints |
| **Security** | Internal only | Public-safe |
| **Debugging** | Full context | Limited context |

### When to Use Each

**Use ServiceResult when:**
- Implementing business logic in service classes
- Need full exception context for debugging and logging
- Handling internal operations between layers
- Chaining multiple service calls
- Working in the business logic or data access layer

**Use ApiResult when:**
- Returning responses from API controllers
- Exposing data through public APIs
- Need to hide internal implementation details from clients
- Converting from ServiceResult for public consumption
- Working in the presentation/controller layer

### Typical Application Architecture

```
┌─────────────────────────────────────┐
│         Controller Layer            │
│      (Uses ApiResult<T>)            │
│  - Returns to clients               │
│  - Sanitized errors                 │
└──────────────┬──────────────────────┘
               │ Convert
               │ ServiceResult → ApiResult
               ↓
┌─────────────────────────────────────┐
│          Service Layer              │
│      (Uses ServiceResult<T>)        │
│  - Business logic                   │
│  - Full error context               │
└──────────────┬──────────────────────┘
               │
               ↓
┌─────────────────────────────────────┐
│       Repository Layer              │
│      (Uses ServiceResult<T>)        │
│  - Data access                      │
│  - Database operations              │
└─────────────────────────────────────┘
```

## Generic vs Non-Generic Variants

Both ServiceResult and ApiResult come in two flavors:

### Generic: `ServiceResult<T>` / `ApiResult<T>`

For operations that return a value:

```csharp
public async Task<ServiceResult<User>> GetUserAsync(Guid id)
{
    var user = await _repository.FindAsync(id);
    if (user == null)
        return ServiceResult.Failure<User>("Not found", MyAppStatus.NotFound);

    return user; // Value is included
}
```

### Non-Generic: `ServiceResult` / `ApiResult`

For operations that only indicate success or failure:

```csharp
public async Task<ServiceResult> DeleteUserAsync(Guid id)
{
    var user = await _repository.FindAsync(id);
    if (user == null)
        return ServiceResult.Failure("Not found", MyAppStatus.NotFound);

    await _repository.DeleteAsync(user);
    return ServiceResult.Success(); // No value needed
}
```

## Status Code Enums

Status codes are represented by enums you define. This provides type safety and clear intent.

### Defining Status Codes

```csharp
[GenerateApiResult]
[GenerateServiceResult]
public enum MyAppStatus
{
    Ok = 0,               // Required: represents success
    BadRequest = 400,     // Validation errors
    Unauthorized = 401,   // Authentication required
    Forbidden = 403,      // Not authorized
    NotFound = 404,       // Resource not found
    InternalServerError = 500  // Unexpected errors
}
```

### Why Use Custom Status Codes?

1. **Type Safety**: Compiler ensures you use valid status codes
2. **Domain-Driven**: Status codes match your business domain
3. **Flexibility**: Not constrained by HTTP status codes
4. **Consistency**: Same codes across all layers

### Enum Values vs HTTP Status Codes

Your enum values don't have to match HTTP status codes:

```csharp
[GenerateApiResult]
[EnableApiResultMapping]  // For ASP.NET Core
public enum MyAppStatus
{
    [HttpStatusCode(HttpStatusCode.OK)]
    Success = 0,          // Enum value is 0, HTTP status is 200

    [HttpStatusCode(HttpStatusCode.NotFound)]
    UserNotFound = 1,     // Enum value is 1, HTTP status is 404

    [HttpStatusCode(HttpStatusCode.BadRequest)]
    InvalidInput = 2      // Enum value is 2, HTTP status is 400
}
```

The `[HttpStatusCode]` attribute (from BPITS.Results.AspNetCore) maps your custom codes to HTTP status codes.

## Success and Failure States

Every result is in one of two states:

### Success State

- `IsSuccess` = true
- `IsFailure` = false
- `Value` = the successful value (for generic results)
- `StatusCode` = typically the "Ok" enum value
- `ErrorMessage` = null
- `ErrorDetails` = null

### Failure State

- `IsSuccess` = false
- `IsFailure` = true
- `Value` = null or default
- `StatusCode` = the error status code
- `ErrorMessage` = description of the error
- `ErrorDetails` = optional field-level validation errors (for validation failures)
- `Exception` = captured exception (ServiceResult only)

## Mental Model

Think of results as boxes that contain either a value or an error:

```
Success Box:
┌─────────────────────────┐
│ ✓ IsSuccess: true       │
│ Value: User{...}        │
│ StatusCode: Ok          │
└─────────────────────────┘

Failure Box:
┌─────────────────────────┐
│ ✗ IsFailure: true       │
│ ErrorMessage: "Not found"│
│ StatusCode: NotFound    │
└─────────────────────────┘
```

You unbox the result by checking the state and extracting the value or handling the error.

## Implicit Conversions

BPITS.Results supports implicit conversions for cleaner code:

### Value to Success Result

```csharp
public ServiceResult<User> GetUser()
{
    var user = new User { Name = "John" };
    return user; // Implicitly converts to ServiceResult.Success(user)
}
```

### ServiceResult to ApiResult

```csharp
public ApiResult<UserDto> GetUser()
{
    ServiceResult<User> serviceResult = GetUserFromService();

    // Implicit conversion
    ApiResult<User> apiResult = serviceResult;

    // Usually combined with MapValue for DTO transformation
    return ApiResult.FromServiceResult(serviceResult.MapValue(u => u?.ToDto()));
}
```

## Key Properties and Methods

### Common Properties

```csharp
bool IsSuccess         // True if operation succeeded
bool IsFailure         // True if operation failed
TValue? Value          // The success value (null on failure)
TEnum StatusCode       // The status code
string? ErrorMessage   // Error description (null on success)
Dictionary<string, string[]>? ErrorDetails  // Validation errors (optional)
```

### ServiceResult-Only Properties

```csharp
Exception? Exception   // Captured exception (null if no exception)
```

### Common Methods

```csharp
bool TryGet(out TValue value)              // Safe value extraction
TValue Get()                               // Value extraction (throws if null)
ServiceResult<TNew> MapValue<TNew>(...)    // Transform value type
ServiceResult<TNew> PassThroughFail<TNew>() // Propagate failure
```

See the [Working with Results](../guides/working-with-results.md) guide for detailed usage.

## Next Steps

Now that you understand the core concepts:

1. **[Working with Results](../guides/working-with-results.md)** - Learn practical usage patterns
2. **[Validation Patterns](../guides/validation-patterns.md)** - Handle validation errors
3. **[Controller Patterns](../guides/controller-patterns.md)** - Use results in ASP.NET Core
4. **[ASP.NET Core Integration](../guides/aspnetcore-integration.md)** - Enable automatic HTTP status mapping

## See Also

- [Quick Start Guide](quick-start.md) - Hands-on introduction
- [Best Practices](../reference/best-practices.md) - Writing effective Result-based code
- [ServiceResult API Reference](../reference/service-result-api.md) - Complete API documentation
- [ApiResult API Reference](../reference/api-result-api.md) - Complete API documentation
