# Quick Start Guide

Get started with BPITS.Results in 3 steps.

## Prerequisites

- [Install BPITS.Results](installation.md) in your project
- Basic familiarity with C# and ASP.NET Core (for controller examples)

## Step 1: Define Your Status Code Enum

Create an enum that represents the possible status codes in your application. Apply the `GenerateApiResult` and/or `GenerateServiceResult` attributes to enable source generation.

The enum **must** include an `Ok` value representing success.

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
    InternalServerError = 500
}
```

### What Just Happened?

The source generator creates:
- `ServiceResult` and `ServiceResult<T>` classes with `MyAppStatus` as the status code type
- `ApiResult` and `ApiResult<T>` classes with `MyAppStatus` as the status code type
- Factory methods like `ServiceResult.Success()`, `ServiceResult.Failure()`, etc.

### Customizing Status Codes

You can customize which enum values are used for common scenarios:

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

## Step 2: Use Results in Your Services

Return `ServiceResult<T>` from your service methods instead of throwing exceptions or returning nulls.

```csharp
public class UserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<User>> GetUserAsync(Guid userId)
    {
        try
        {
            var user = await _repository.FindAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure<User>(
                    "User not found",
                    MyAppStatus.ResourceNotFound
                );
            }

            // Implicit conversion to ServiceResult.Success(user)
            return user;

            // Or use explicit syntax:
            // return ServiceResult.Success(user);
        }
        catch (Exception ex)
        {
            // Capture exception details for internal debugging
            return ServiceResult.Failure<User>(ex, "Failed to retrieve user");
        }
    }

    public async Task<ServiceResult<User>> CreateUserAsync(CreateUserRequest request)
    {
        // Validate input
        if (string.IsNullOrEmpty(request.Email))
        {
            return ServiceResult.ValidationFailure<User>(
                nameof(request.Email),
                "Email is required"
            );
        }

        try
        {
            var user = new User
            {
                Email = request.Email,
                Name = request.Name
            };

            await _repository.AddAsync(user);
            return user;
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure<User>(ex, "Failed to create user");
        }
    }
}
```

### Why ServiceResult in Services?

- **Full error context**: Includes exception details for debugging
- **No exceptions for control flow**: Explicit success/failure states
- **Rich error information**: Status codes, error messages, and validation details

## Step 3: Use Results in Your Controllers

Convert `ServiceResult` to `ApiResult` in your controllers and return them to clients.

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<ApiResult<UserDto>> GetUser(Guid id)
    {
        var result = await _userService.GetUserAsync(id);

        // Convert ServiceResult to ApiResult and map to DTO
        return ApiResult.FromServiceResult(result.MapValue(user => user?.ToDto()));

        // The conversion:
        // - Removes exception details (not safe for public APIs)
        // - Preserves status code and error message
        // - Transforms the value type (User -> UserDto)
    }

    [HttpPost]
    public async Task<ApiResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request);
        return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
    }

    [HttpGet]
    public async Task<ApiResult<List<UserDto>>> GetAllUsers()
    {
        var result = await _userService.GetAllUsersAsync();

        // Transform list of users to list of DTOs
        return ApiResult.FromServiceResult(
            result.MapValue(users => users?.Select(u => u.ToDto()).ToList())
        );
    }
}
```

### Why ApiResult in Controllers?

- **Public-safe errors**: Exception details are excluded
- **Clean API responses**: Sanitized error messages
- **Consistent format**: All endpoints return the same structure

## Complete Example

Here's a complete example putting it all together:

```csharp
// 1. Define status code enum
[GenerateApiResult(
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(BadRequest)
)]
[GenerateServiceResult(
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(BadRequest)
)]
public enum MyAppStatus
{
    Ok = 0,
    BadRequest = 400,
    Unauthorized = 401,
    NotFound = 404,
    InternalServerError = 500
}

// 2. Service layer
public class ProductService
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductService> _logger;

    public async Task<ServiceResult<Product>> GetProductAsync(int productId)
    {
        try
        {
            var product = await _repository.GetByIdAsync(productId);
            if (product == null)
                return ServiceResult.Failure<Product>(
                    $"Product with ID {productId} not found",
                    MyAppStatus.NotFound
                );

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}", productId);
            return ServiceResult.Failure<Product>(ex, "Failed to retrieve product");
        }
    }

    public async Task<ServiceResult<Product>> UpdatePriceAsync(int productId, decimal newPrice)
    {
        if (newPrice <= 0)
            return ServiceResult.ValidationFailure<Product>(
                nameof(newPrice),
                "Price must be greater than zero"
            );

        var productResult = await GetProductAsync(productId);
        if (!productResult.TryGet(out var product))
            return productResult; // Propagate the failure

        product.Price = newPrice;
        await _repository.UpdateAsync(product);
        return product;
    }
}

// 3. Controller layer
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    [HttpGet("{id}")]
    public async Task<ApiResult<ProductDto>> GetProduct(int id)
    {
        var result = await _productService.GetProductAsync(id);
        return ApiResult.FromServiceResult(result.MapValue(p => p?.ToDto()));
    }

    [HttpPut("{id}/price")]
    public async Task<ApiResult<ProductDto>> UpdatePrice(
        int id,
        [FromBody] UpdatePriceRequest request)
    {
        var result = await _productService.UpdatePriceAsync(id, request.NewPrice);
        return ApiResult.FromServiceResult(result.MapValue(p => p?.ToDto()));
    }
}
```

## What You Get

When using BPITS.Results, your API responses will be consistent:

### Success Response (HTTP 200)
```json
{
  "value": {
    "id": 123,
    "name": "Product Name",
    "price": 29.99
  },
  "isSuccess": true,
  "isFailure": false,
  "statusCode": 0,
  "errorMessage": null,
  "errorDetails": null
}
```

### Failure Response (HTTP 404)
```json
{
  "value": null,
  "isSuccess": false,
  "isFailure": true,
  "statusCode": 404,
  "errorMessage": "Product with ID 123 not found",
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
    "newPrice": ["Price must be greater than zero"]
  }
}
```

## Common Questions

### Do I always need both ServiceResult and ApiResult?

Not necessarily:
- **Service-only applications**: Use only `ServiceResult`
- **APIs**: Use `ServiceResult` in services, `ApiResult` in controllers
- **Simple scenarios**: You can use `ApiResult` throughout if you don't need exception details

### Can I use results with existing code?

Yes! BPITS.Results works alongside traditional exception-based code. Gradually migrate as needed.

### What about performance?

BPITS.Results uses source generation with zero runtime overhead. The generated code is as performant as hand-written code.

## Troubleshooting

### "ServiceResult does not exist in the current context"

Make sure:
1. You've added the `[GenerateServiceResult]` attribute to your enum
2. Your project has built successfully (source generators run during build)
3. You've added `using BPITS.Results.Abstractions;` at the top of your file

### "Cannot implicitly convert type T to ServiceResult<T>"

The implicit conversion requires the result to be successful. If you're not sure, use explicit syntax:
```csharp
return ServiceResult.Success(value);
```

## Related

- [Core Concepts](core-concepts.md) - Deeper understanding of the Result pattern
- [Installation](installation.md) - Installation troubleshooting
- [Working with Results](../guides/working-with-results.md) - Usage patterns
- [ASP.NET Core Integration](../guides/aspnetcore-integration.md) - Automatic HTTP status code mapping
