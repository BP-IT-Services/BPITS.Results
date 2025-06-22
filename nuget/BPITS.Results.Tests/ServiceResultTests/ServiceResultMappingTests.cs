using BPITS.Results.Tests.Scaffolding;

namespace BPITS.Results.Tests.ServiceResultTests;

public class ServiceResultMappingTests
{
    [Fact]
    public void MapValue_WithSuccessResult_TransformsValue()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "John", "john@example.com");
        var result = ServiceResult.Success(user);
        
        // Act
        var dtoResult = result.MapValue(u => u?.ToDto());
        
        // Assert
        Assert.Equal(TestStatusCode.Ok, dtoResult.StatusCode);
        Assert.NotNull(dtoResult.Value);
        Assert.IsType<UserDto>(dtoResult.Value);
        Assert.Equal(user.Id, dtoResult.Value.Id);
        Assert.Equal(user.Name, dtoResult.Value.Name);
    }
    
    [Fact]
    public void MapValue_WithFailureResult_PreservesError()
    {
        // Arrange
        var result = ServiceResult.Failure<User>("User not found", TestStatusCode.ResourceNotFound);
        
        // Act
        var dtoResult = result.MapValue(u => u?.ToDto());
        
        // Assert
        Assert.Equal(TestStatusCode.ResourceNotFound, dtoResult.StatusCode);
        Assert.Equal("User not found", dtoResult.ErrorMessage);
        Assert.Null(dtoResult.Value);
        Assert.False(dtoResult.IsSuccess);
    }
    
    [Fact]
    public void MapValue_WithNullValue_HandlesCorrectly()
    {
        // Arrange
        var result = ServiceResult.Success<User>(null);
        
        // Act
        var dtoResult = result.MapValue(u => u?.ToDto());
        
        // Assert
        Assert.Equal(TestStatusCode.Ok, dtoResult.StatusCode);
        Assert.Null(dtoResult.Value);
        Assert.True(dtoResult.IsSuccess);
    }
    
    [Fact]
    public void MapValueWhenNotNull_WithValue_TransformsValue()
    {
        // Arrange
        var school = new School(Guid.NewGuid(), "Test School", "123 Main St");
        var result = ServiceResult.Success(school);
        
        // Act
        var dtoResult = result.MapValueWhenNotNull(s => s.ToDto());
        
        // Assert
        Assert.NotNull(dtoResult.Value);
        Assert.Equal(school.Id, dtoResult.Value.Id);
        Assert.Equal(school.Name, dtoResult.Value.Name);
    }
    
    [Fact]
    public void MapValueWhenNotNull_WithNull_ReturnsDefault()
    {
        // Arrange
        var result = ServiceResult.Success<School>(null);
        
        // Act
        var dtoResult = result.MapValueWhenNotNull(s => s.ToDto());
        
        // Assert
        Assert.Null(dtoResult.Value);
        Assert.True(dtoResult.IsSuccess);
    }
    
    [Fact]
    public void MapValue_WithDifferentFunctions_AppliesCorrectly()
    {
        // Arrange
        var resultWithValue = ServiceResult.Success(new User(Guid.NewGuid(), "John", "john@example.com"));
        var resultWithNull = ServiceResult.Success<User>(null);
        
        // Act
        var mapped1 = resultWithValue.MapValue(
            whenValueNotNullFunc: u => u.Name,
            whenValueNullFunc: _ => "Unknown"
        );
        
        var mapped2 = resultWithNull.MapValue(
            whenValueNotNullFunc: u => u.Name,
            whenValueNullFunc: _ => "Unknown"
        );
        
        // Assert
        Assert.Equal("John", mapped1.Value);
        Assert.Equal("Unknown", mapped2.Value);
    }
    
    [Fact]
    public void MapValue_ChainedTransformations_WorkCorrectly()
    {
        // Arrange
        var job = new Job(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now, 3);
        var result = ServiceResult.Success(job);
        
        // Act - Chain transformations like in ROSCO examples
        var finalResult = result
            .MapValue(j => j?.ToDto())
            .MapValue(dto => dto?.ShootDate)
            .MapValue(date => date?.Replace("-", "/"));
        
        // Assert
        Assert.True(finalResult.IsSuccess);
        Assert.NotNull(finalResult.Value);
        Assert.Contains("/", finalResult.Value);
    }
    
    [Fact]
    public void MapValue_PreservesErrorDetails()
    {
        // Arrange
        var errorDetails = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Invalid format" } }
        };
        var exception = new InvalidOperationException("Test");
        var result = ServiceResult.Failure<User>(exception, "Validation failed", TestStatusCode.BadRequest, null, errorDetails);
        
        // Act
        var mappedResult = result.MapValue(u => u?.ToDto());
        
        // Assert
        Assert.Equal(TestStatusCode.BadRequest, mappedResult.StatusCode);
        Assert.Equal("Validation failed", mappedResult.ErrorMessage);
        Assert.Equal(errorDetails, mappedResult.ErrorDetails);
        Assert.Equal(exception, mappedResult.InnerException);
    }
    
    [Fact]
    public void NonGenericMapValue_TransformsCorrectly()
    {
        // Arrange
        var result = ServiceResult.Success(); // Non-generic
        
        // Act
        var mappedResult = result.MapValue(() => "Success!");
        
        // Assert
        Assert.Equal("Success!", mappedResult.Value);
        Assert.True(mappedResult.IsSuccess);
    }
    
    [Fact]
    public void RealWorldScenario_PagedResultTransformation()
    {
        // Arrange - Similar to ROSCO's CalendarService
        var users = new List<User>
        {
            new(Guid.NewGuid(), "User1", "user1@example.com"),
            new(Guid.NewGuid(), "User2", "user2@example.com")
        };
        var pagedResult = new PagedResult<User>(users, 2, 0, 10);
        var serviceResult = ServiceResult.Success(pagedResult);
        
        // Act - Transform like in ROSCO controllers
        var apiResult = serviceResult.MapValue(paged => paged?.Select(u => u.ToDto()));
        
        // Assert
        Assert.True(apiResult.IsSuccess);
        Assert.NotNull(apiResult.Value);
        Assert.Equal(2, apiResult.Value.Items.Count);
        Assert.All(apiResult.Value.Items, dto => Assert.IsType<UserDto>(dto));
    }
    
    [Fact]
    public void ComplexMapping_WithConditionalLogic()
    {
        // Arrange
        var resultWithFullData = ServiceResult.Success(new User(Guid.NewGuid(), "John Doe", "john@example.com"));
        var resultWithPartialData = ServiceResult.Success(new User(Guid.NewGuid(), "", ""));
        
        // Act
        var mapped1 = resultWithFullData.MapValue(user =>
            !string.IsNullOrEmpty(user?.Name) ? user.Name : "Anonymous"
        );
        
        var mapped2 = resultWithPartialData.MapValue(user =>
            !string.IsNullOrEmpty(user?.Name) ? user.Name : "Anonymous"
        );
        
        // Assert
        Assert.Equal("John Doe", mapped1.Value);
        Assert.Equal("Anonymous", mapped2.Value);
    }
}