# Controller Patterns

Best practices and common patterns for using BPITS.Results in ASP.NET Core controllers.

## Table of Contents

- [Overview](#overview)
- [Basic Controller Actions](#basic-controller-actions)
- [Paged Results](#paged-results)
- [Exception Handling](#exception-handling)
- [Different HTTP Methods](#different-http-methods)
- [Testing Controller Actions](#testing-controller-actions)
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

## Different HTTP Methods

### PATCH - Partial Update

```csharp
[HttpPatch("{id}")]
public async Task<ApiResult<UserDto>> PatchUser(
    Guid id,
    [FromBody] JsonPatchDocument<UpdateUserRequest> patchDoc)
{
    // Get existing user
    var getUserResult = await _userService.GetUserAsync(id);
    if (!getUserResult.TryGet(out var user))
        return ApiResult.FromServiceResult(getUserResult.MapValue<UserDto>(_ => null));

    // Apply patch
    var request = new UpdateUserRequest();
    // Map from user to request
    patchDoc.ApplyTo(request);

    // Update
    var updateResult = await _userService.UpdateUserAsync(id, request);
    return ApiResult.FromServiceResult(updateResult.MapValue(u => u?.ToDto()));
}
```

### HEAD - Check Existence

```csharp
[HttpHead("{id}")]
public async Task<ApiResult> CheckUserExists(Guid id)
{
    var result = await _userService.UserExistsAsync(id);
    return ApiResult.FromServiceResult(result);
}
```

## Testing Controller Actions

### Unit Testing Controllers

```csharp
public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UsersController(_userServiceMock.Object);
    }

    [Fact]
    public async Task GetUser_WhenUserExists_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "John" };
        _userServiceMock
            .Setup(s => s.GetUserAsync(userId))
            .ReturnsAsync(ServiceResult.Success(user));

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("John", result.Value.Name);
    }

    [Fact]
    public async Task GetUser_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock
            .Setup(s => s.GetUserAsync(userId))
            .ReturnsAsync(ServiceResult.Failure<User>(
                "User not found",
                MyAppStatus.NotFound
            ));

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(MyAppStatus.NotFound, result.StatusCode);
        Assert.Equal("User not found", result.ErrorMessage);
    }
}
```

### Integration Testing

```csharp
public class UsersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsersControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUser_Returns200WithUser()
    {
        // Act
        var response = await _client.GetAsync("/api/users/some-guid");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResult<UserDto>>(content);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task GetUser_Returns404WhenNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/users/non-existent-id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResult<UserDto>>(content);

        Assert.True(result.IsFailure);
        Assert.Equal(MyAppStatus.NotFound, result.StatusCode);
    }
}
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

### 4. Use Meaningful Route Names

```csharp
// Good: RESTful routes
[HttpGet("{id}")]                          // GET /api/users/123
[HttpPost]                                 // POST /api/users
[HttpPut("{id}")]                          // PUT /api/users/123
[HttpDelete("{id}")]                       // DELETE /api/users/123
[HttpGet("{id}/orders")]                   // GET /api/users/123/orders

// Avoid: RPC-style routes
[HttpGet("GetUserById/{id}")]              // Avoid
[HttpPost("CreateNewUser")]                // Avoid
```

### 5. Use ApiController Attribute

```csharp
// Good: Use [ApiController] attribute
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // Automatic model validation
    // Automatic 400 responses for invalid models
    // Binding source parameter inference
}

// Without [ApiController], you lose these features
```

### 6. Leverage Model Binding

```csharp
// Good: Use model binding attributes
[HttpGet("{id}")]
public async Task<ApiResult<UserDto>> GetUser(Guid id) { }

[HttpPost]
public async Task<ApiResult<UserDto>> CreateUser([FromBody] CreateUserRequest request) { }

[HttpGet]
public async Task<ApiResult<List<UserDto>>> Search([FromQuery] string term) { }

// ASP.NET Core will automatically bind and validate
```

## See Also

- [Working with Results](working-with-results.md) - Core result operations
- [Validation Patterns](validation-patterns.md) - Handling validation in services
- [ASP.NET Core Integration](aspnetcore-integration.md) - HTTP status code mapping
- [Best Practices](../reference/best-practices.md) - Comprehensive best practices
- [Dependency Injection](../integration/dependency-injection.md) - Testing patterns

## Next Steps

- Set up [ASP.NET Core Integration](aspnetcore-integration.md) for automatic HTTP status mapping
- Learn about [Testing patterns](../integration/dependency-injection.md#testing-with-results)
- Review [Best Practices](../reference/best-practices.md) for production-ready code
