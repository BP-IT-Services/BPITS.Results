# Controller Patterns

Patterns for using BPITS.Results in ASP.NET Core controllers.

## Table of Contents

- [Overview](#overview)
- [Basic Controller Actions](#basic-controller-actions)
- [Paged Results](#paged-results)
- [Exception Handling](#exception-handling)
- [Best Practices](#best-practices)

## Overview

When using BPITS.Results in ASP.NET Core controllers, follow these principles:

1. Use **ServiceResult** in your service layer
2. Convert to **ApiResult** in your controller
3. Transform entities to DTOs during conversion
4. Let ApiResult handle the HTTP response

## Basic Controller Actions

### GET - Retrieve Single Item

```csharp
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
        return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
    }
}
```

### GET - Retrieve List

```csharp
[HttpGet]
public async Task<ApiResult<List<UserDto>>> GetAllUsers()
{
    var result = await _userService.GetAllUsersAsync();

    return ApiResult.FromServiceResult(
        result.MapValue(users => users?.Select(u => u.ToDto()).ToList())
    );
}
```

### POST - Create Resource

```csharp
[HttpPost]
public async Task<ApiResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
{
    var result = await _userService.CreateUserAsync(request);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}
```

### PUT - Update Resource

```csharp
[HttpPut("{id}")]
public async Task<ApiResult<UserDto>> UpdateUser(
    Guid id,
    [FromBody] UpdateUserRequest request)
{
    var result = await _userService.UpdateUserAsync(id, request);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}
```

### DELETE - Remove Resource

```csharp
[HttpDelete("{id}")]
public async Task<ApiResult> DeleteUser(Guid id)
{
    // Non-generic ApiResult (no value returned)
    var result = await _userService.DeleteUserAsync(id);
    return ApiResult.FromServiceResult(result);
}
```

## Paged Results

Handling paginated data:

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

[HttpGet]
public async Task<ApiResult<PagedResult<UserDto>>> GetUsersPaged(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
{
    var result = await _userService.GetUsersPagedAsync(pageNumber, pageSize);

    if (result.TryGet(out var pagedResult))
    {
        // Transform the items in the paged result
        var dtoPagedResult = new PagedResult<UserDto>
        {
            Items = pagedResult.Items.Select(u => u.ToDto()).ToList(),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };

        return ApiResult.Success(dtoPagedResult);
    }

    return ApiResult.FromServiceResult(result.MapValue<PagedResult<UserDto>>(_ => null));
}
```

Alternative using MapValue:

```csharp
[HttpGet]
public async Task<ApiResult<PagedResult<UserDto>>> GetUsersPaged(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
{
    var result = await _userService.GetUsersPagedAsync(pageNumber, pageSize);

    return ApiResult.FromServiceResult(
        result.MapValue(paged => paged == null ? null : new PagedResult<UserDto>
        {
            Items = paged.Items.Select(u => u.ToDto()).ToList(),
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        })
    );
}
```

## Exception Handling

### Try-Catch in Controllers

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
        // Log the exception
        _logger.LogError(ex, "Unexpected error creating user");

        // Return a safe error message to the client
        return ApiResult.Failure<UserDto>(
            "An unexpected error occurred while creating the user",
            MyAppStatus.InternalServerError
        );
    }
}
```

**Note**: If your service layer properly returns `ServiceResult`, you shouldn't need try-catch in controllers. Use it only for truly unexpected exceptions that escape the service layer.

### Global Exception Handling (Recommended)

Instead of try-catch in every controller, use middleware:

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var errorResult = new
            {
                isSuccess = false,
                isFailure = true,
                errorMessage = "An unexpected error occurred",
                statusCode = 500
            };

            await context.Response.WriteAsJsonAsync(errorResult);
        }
    }
}

// Register in Program.cs
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

## Best Practices

### 1. Always Convert to ApiResult in Controllers

```csharp
// Good: ServiceResult converted to ApiResult
[HttpGet("{id}")]
public async Task<ApiResult<UserDto>> GetUser(Guid id)
{
    var serviceResult = await _userService.GetUserAsync(id);
    return ApiResult.FromServiceResult(serviceResult.MapValue(u => u?.ToDto()));
}

// Bad: Returning ServiceResult directly (exposes exceptions)
[HttpGet("{id}")]
public async Task<ServiceResult<User>> GetUser(Guid id)
{
    return await _userService.GetUserAsync(id); // Don't do this!
}
```

### 2. Transform to DTOs in Controller

```csharp
// Good: Transform entities to DTOs
[HttpGet("{id}")]
public async Task<ApiResult<UserDto>> GetUser(Guid id)
{
    var result = await _userService.GetUserAsync(id);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}

// Bad: Exposing entities directly
[HttpGet("{id}")]
public async Task<ApiResult<User>> GetUser(Guid id)
{
    var result = await _userService.GetUserAsync(id);
    return ApiResult.FromServiceResult(result); // Exposes internal structure
}
```

### 3. Keep Controllers Thin

```csharp
// Good: Controller delegates to service
[HttpPost]
public async Task<ApiResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
{
    var result = await _userService.CreateUserAsync(request);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}

// Bad: Business logic in controller
[HttpPost]
public async Task<ApiResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
{
    // Validation
    if (string.IsNullOrEmpty(request.Email))
        return ApiResult.ValidationFailure<UserDto>("Email", "Required");

    // Business logic
    var user = new User { Email = request.Email };
    await _repository.AddAsync(user);

    return ApiResult.Success(user.ToDto());
}
```

## Related

- [Working with Results](working-with-results.md) - Core result operations
- [Validation Patterns](validation-patterns.md) - Handling validation in services
- [ASP.NET Core Integration](aspnetcore-integration.md) - HTTP status code mapping
- [Dependency Injection](../integration/dependency-injection.md) - Testing patterns
