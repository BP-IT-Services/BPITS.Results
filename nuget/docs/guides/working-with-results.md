# Working with Results

Creating, extracting, and transforming results in BPITS.Results.

## Table of Contents

- [Creating Results](#creating-results)
  - [Success Results](#success-results)
  - [Failure Results](#failure-results)
- [Extracting Values Safely](#extracting-values-safely)
  - [TryGet Method (Recommended)](#tryget-method-recommended)
  - [Direct Property Access](#direct-property-access)
  - [Get Method](#get-method)
- [Type Conversion with MapValue](#type-conversion-with-mapvalue)
  - [Basic Usage](#basic-usage)
  - [Handling Null Values](#handling-null-values)
  - [Chaining Transformations](#chaining-transformations)
- [Converting Between Result Types](#converting-between-result-types)
  - [ServiceResult to ApiResult](#serviceresult-to-apiresult)
  - [Overriding Error Information](#overriding-error-information)
- [Chaining Operations](#chaining-operations)

## Creating Results

### Success Results

Success results indicate that an operation completed successfully and may include a value.

#### Generic Success (With Value)

```csharp
// Explicit creation
var userResult = ServiceResult.Success(user);
var apiUserResult = ApiResult.Success(userDto);

// Implicit conversion from value (recommended for cleaner code)
ServiceResult<User> result = user;
ApiResult<UserDto> apiResult = userDto;

// Example in a method
public ServiceResult<User> GetUser(Guid id)
{
    var user = _repository.Find(id);
    if (user == null)
        return ServiceResult.Failure<User>("Not found", MyAppStatus.NotFound);

    return user; // Implicit conversion to Success
}
```

#### Non-Generic Success (No Value)

Use when you only need to indicate success without returning a value:

```csharp
// Explicit creation
var operationResult = ServiceResult.Success();
var apiOperationResult = ApiResult.Success();

// Example: Delete operation
public async Task<ServiceResult> DeleteUserAsync(Guid id)
{
    var user = await _repository.FindAsync(id);
    if (user == null)
        return ServiceResult.Failure("User not found", MyAppStatus.NotFound);

    await _repository.DeleteAsync(user);
    return ServiceResult.Success(); // No value needed
}
```

### Failure Results

Failure results indicate an operation failed and include error information.

#### Basic Failure

```csharp
// Generic failure (when you need the type for return type consistency)
var result = ServiceResult.Failure<User>(
    "User not found",
    MyAppStatus.ResourceNotFound
);

// Non-generic failure
var result = ServiceResult.Failure(
    "Operation failed",
    MyAppStatus.InternalServerError
);

// ApiResult failure
var apiResult = ApiResult.Failure<UserDto>(
    "User not found",
    MyAppStatus.ResourceNotFound
);
```

#### Failure with Exception (ServiceResult Only)

ServiceResult can capture exception details for internal debugging:

```csharp
try
{
    // Operation that might throw
    var result = await _repository.SaveAsync(user);
    return ServiceResult.Success(result);
}
catch (DbUpdateException ex)
{
    // Capture exception with context
    return ServiceResult.Failure<User>(
        ex,
        "Failed to save user to database",
        MyAppStatus.InternalServerError
    );
}
```

The exception details are available in `result.Exception` but are **NOT** included when converting to ApiResult (for security).

#### Validation Failure

For validation errors, use the special validation methods:

```csharp
// Single field validation error
var result = ServiceResult.ValidationFailure<User>(
    "Email",
    "Email address is required"
);

// Multiple errors for a single field
var result = ServiceResult.ValidationFailure<User>(
    "Email",
    new[] { "Email is required", "Email must be valid" }
);

// Multiple field validation errors
var result = ServiceResult.ValidationFailure<User>(
    new Dictionary<string, string[]>
    {
        { "Email", new[] { "Email is required" } },
        { "Password", new[] { "Password must be at least 8 characters" } },
        { "Name", new[] { "Name is required" } }
    }
);

// With a general error message
var result = ServiceResult.ValidationFailure<User>(
    "Validation failed",
    new Dictionary<string, string[]>
    {
        { "Email", new[] { "Invalid format" } }
    }
);
```

See the [Validation Patterns](validation-patterns.md) guide for more details.

## Extracting Values Safely

### TryGet Method (Recommended)

The `TryGet` method is the recommended way to extract values because it eliminates null checks and provides a clear pattern:

```csharp
public async Task<ApiResult<UserDto>> GetUser(Guid id)
{
    var serviceResult = await _userService.GetUserAsync(id);

    // Safe value extraction with TryGet
    if (serviceResult.TryGet(out var user))
    {
        // user is guaranteed to be non-null here
        return user.ToDto(); // Implicit conversion to ApiResult.Success
    }

    // Handle failure case
    return ApiResult.FromServiceResult(serviceResult.MapValue<UserDto>(_ => null));
}
```

**Why TryGet is recommended:**
- Compiler-enforced null safety
- Clear success/failure branching
- No manual null checking needed
- Follows familiar C# patterns (like `TryParse`, `TryGetValue`)

### Direct Property Access

You can access the `Value` property directly, but you must check for null:

```csharp
var result = await _userService.GetUserAsync(id);

if (result.IsSuccess && result.Value != null)
{
    var user = result.Value;
    // Work with user
}
else
{
    // Handle failure
    Console.WriteLine(result.ErrorMessage);
}
```

**When to use:**
- When you need to check `IsSuccess` for other reasons
- When working with non-nullable value types where null isn't a concern

### Get Method

The `Get()` method extracts the value or throws an exception if the value is null:

```csharp
try
{
    var result = await _userService.GetUserAsync(id);
    var user = result.Get(); // Throws ArgumentNullException if Value is null

    // Work with user
}
catch (ArgumentNullException)
{
    // Handle null value case
}
```

**When to use:**
- Rarely - only in scenarios where null should never occur and represents a programming error
- When you want an exception for unexpected null values

**Warning**: This method throws an exception, which goes against the Result pattern philosophy. Use `TryGet` instead when possible.

## Type Conversion with MapValue

`MapValue` allows you to transform the result's value type while preserving status codes and error information.

### Basic Usage

```csharp
// Convert entity to DTO
var userResult = await _userService.GetUserAsync(id);
var userDtoResult = userResult.MapValue(user => user?.ToDto());

// The status code and error information are preserved:
// - If userResult was successful, userDtoResult is successful with the DTO
// - If userResult failed, userDtoResult fails with the same error

// Example: Service returns User, Controller returns UserDto
public async Task<ApiResult<UserDto>> GetUser(Guid id)
{
    var result = await _userService.GetUserAsync(id); // Returns ServiceResult<User>
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}
```

### Handling Null Values

MapValue provides special methods for handling null values:

#### MapValueWhenNotNull

Only transforms if the value is not null:

```csharp
var result = serviceResult.MapValueWhenNotNull(user => user.ToDto());

// Equivalent to:
var result = serviceResult.MapValue(user => user?.ToDto());
```

#### Custom Null Handling

Provide different functions for null and non-null cases:

```csharp
var result = serviceResult.MapValue(
    whenValueNotNullFunc: user => user.ToDetailedDto(),
    whenValueNullFunc: _ => new UserDto { Name = "Unknown" }
);

// whenValueNotNullFunc is called if Value is not null
// whenValueNullFunc is called if Value is null
```

This is useful when you need a default value instead of null.

### Chaining Transformations

You can chain multiple `MapValue` calls for multi-step transformations:

```csharp
var finalResult = serviceResult
    .MapValue(user => user?.ToDto())              // User -> UserDto
    .MapValue(dto => dto?.ToApiModel())           // UserDto -> ApiModel
    .MapValue(model => model?.ToViewModel());     // ApiModel -> ViewModel

// Each transformation preserves errors from previous steps
// If any step has a null value and you're using ?., subsequent steps receive null
// The status code and error message flow through all transformations
```

**Example: Complex transformation pipeline**

```csharp
public async Task<ApiResult<OrderViewModel>> GetOrderViewModel(Guid orderId)
{
    var orderResult = await _orderService.GetOrderAsync(orderId);

    return ApiResult.FromServiceResult(
        orderResult
            .MapValue(order => order?.EnrichWithCustomerData())
            .MapValue(enriched => enriched?.ToDto())
            .MapValue(dto => dto?.ToViewModel())
    );
}
```

### MapValue vs TryGet

```csharp
// Using TryGet (when you need branching logic)
if (serviceResult.TryGet(out var user))
{
    var dto = user.ToDto();
    // Additional logic...
    return ApiResult.Success(dto);
}
else
{
    return ApiResult.FromServiceResult(serviceResult.MapValue<UserDto>(_ => null));
}

// Using MapValue (when transformation is straightforward)
return ApiResult.FromServiceResult(serviceResult.MapValue(user => user?.ToDto()));

// MapValue is more concise when you don't need branching logic
// TryGet is better when you need to do different things based on success/failure
```

## Converting Between Result Types

### ServiceResult to ApiResult

Convert ServiceResult to ApiResult when returning from controllers. This removes internal implementation details (like exceptions) that shouldn't be exposed to clients.

#### Explicit Conversion

```csharp
var serviceResult = await _userService.GetUserAsync(id);
var apiResult = ApiResult.FromServiceResult(serviceResult);
```

#### Implicit Conversion

```csharp
ServiceResult<User> serviceResult = await _userService.GetUserAsync(id);
ApiResult<User> apiResult = serviceResult; // Automatic conversion
```

#### Convert with Type Mapping

Most commonly, you'll combine conversion with type transformation:

```csharp
var serviceResult = await _userService.GetUserAsync(id); // ServiceResult<User>
var apiResult = ApiResult.FromServiceResult(
    serviceResult.MapValue(user => user?.ToDto())
);
// Result: ApiResult<UserDto>
```

**What happens during conversion:**
- ✅ Status code is preserved
- ✅ Error message is preserved
- ✅ Error details are preserved
- ✅ Value is preserved (or transformed with MapValue)
- ❌ Exception details are **removed** (security)

### Overriding Error Information

You can override the error message or status code during conversion:

```csharp
var apiResult = ApiResult.FromServiceResult(
    serviceResult,
    errorMessage: "Custom error message for clients",
    statusCode: MyAppStatus.BadRequest
);

// Use case: Provide a more user-friendly error message
var internalResult = await _service.ComplexOperation(); // "Database constraint violation XK_Users_Email"
var apiResult = ApiResult.FromServiceResult(
    internalResult,
    errorMessage: "This email address is already registered" // User-friendly message
);
```

## Chaining Operations

When performing multiple operations that return results, use `TryGet` and `PassThroughFail` to chain them:

```csharp
public async Task<ServiceResult<OrderDto>> CreateOrderAsync(CreateOrderRequest request)
{
    // Step 1: Validate and get user
    var userResult = await _userService.GetUserAsync(request.UserId);
    if (!userResult.TryGet(out var user))
        return userResult.PassThroughFail<OrderDto>(); // Propagate the failure

    // Step 2: Validate and get products
    var productsResult = await _productService.GetProductsAsync(request.ProductIds);
    if (!productsResult.TryGet(out var products))
        return productsResult.PassThroughFail<OrderDto>(); // Propagate the failure

    // Step 3: Validate inventory
    var inventoryCheck = await _inventoryService.CheckAvailabilityAsync(products);
    if (!inventoryCheck.IsSuccess)
        return inventoryCheck.PassThroughFail<OrderDto>();

    // Step 4: Create order (all validations passed)
    var order = new Order(user, products);
    var createResult = await _orderRepository.CreateAsync(order);

    // Step 5: Transform to DTO
    return createResult.MapValue(o => o?.ToDto());
}
```

**Pattern explanation:**
1. Call operation that returns a result
2. Use `TryGet` to check success and extract value
3. If failure, use `PassThroughFail<T>()` to propagate the error with the correct return type
4. If all operations succeed, perform the final operation
5. Transform the final result with `MapValue` if needed

## Best Practices

### 1. Prefer TryGet for Value Extraction

```csharp
// Good: Clear and safe
if (result.TryGet(out var user))
{
    ProcessUser(user);
}

// Avoid: Manual null checking
if (result.IsSuccess && result.Value != null)
{
    ProcessUser(result.Value);
}
```

### 2. Use Implicit Conversions

```csharp
// Good: Clean and concise
public ServiceResult<User> GetUser(Guid id)
{
    var user = _repository.Find(id);
    if (user == null)
        return ServiceResult.Failure<User>("Not found", MyAppStatus.NotFound);

    return user; // Implicit conversion
}

// Acceptable: Explicit when clarity is needed
public ServiceResult<User> GetUser(Guid id)
{
    var user = _repository.Find(id);
    if (user == null)
        return ServiceResult.Failure<User>("Not found", MyAppStatus.NotFound);

    return ServiceResult.Success(user); // Explicit
}
```

### 3. Chain MapValue for Multi-Step Transformations

```csharp
// Good: Clear transformation pipeline
return ApiResult.FromServiceResult(
    serviceResult
        .MapValue(entity => entity?.ToDto())
        .MapValue(dto => dto?.ToViewModel())
);

// Avoid: Manual intermediate variables (unless needed for debugging)
var dto = serviceResult.Value?.ToDto();
var viewModel = dto?.ToViewModel();
return ApiResult.FromServiceResult(ServiceResult.Success(viewModel));
```

### 4. Always Convert ServiceResult to ApiResult in Controllers

```csharp
// Good: Removes internal implementation details
public async Task<ApiResult<UserDto>> GetUser(Guid id)
{
    var serviceResult = await _userService.GetUserAsync(id);
    return ApiResult.FromServiceResult(serviceResult.MapValue(u => u?.ToDto()));
}

// Avoid: Exposing ServiceResult to clients
public async Task<ServiceResult<User>> GetUser(Guid id) // Bad: ServiceResult in controller
{
    return await _userService.GetUserAsync(id);
}
```

## Related

- [Validation Patterns](validation-patterns.md) - Handling validation errors
- [Error Handling](error-handling.md) - Managing errors and status codes
- [Controller Patterns](controller-patterns.md) - Using results in ASP.NET Core
