# Dependency Injection

Patterns for using BPITS.Results with dependency injection and testing.

## Registering Services

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register services that return results
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IOrderService, OrderService>();

        // Register action result mapper
        builder.Services.AddMyAppStatusActionResultMapper();

        var app = builder.Build();
        app.MapControllers();
        app.Run();
    }
}
```

## Testing with Results

### Unit Testing Services

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _service = new UserService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetUserAsync_WhenUserExists_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "John" };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(ServiceResult.Success(user));

        // Act
        var result = await _service.GetUserAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("John", result.Value.Name);
    }

    [Fact]
    public async Task GetUserAsync_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(ServiceResult.Failure<User>("Not found", MyStatus.NotFound));

        // Act
        var result = await _service.GetUserAsync(userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(MyStatus.NotFound, result.StatusCode);
    }
}
```

### Mocking Result-Returning Services

```csharp
public class OrderServiceTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IProductService> _productServiceMock;
    private readonly OrderService _orderService;

    [Fact]
    public async Task CreateOrderAsync_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        _userServiceMock
            .Setup(s => s.GetUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(ServiceResult.Failure<User>("Not found", MyStatus.NotFound));

        // Act
        var result = await _orderService.CreateOrderAsync(new CreateOrderRequest());

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(MyStatus.NotFound, result.StatusCode);
    }
}
```

## Related

- [Controller Patterns](../guides/controller-patterns.md) - Testing controllers
- [Working with Results](../guides/working-with-results.md)
