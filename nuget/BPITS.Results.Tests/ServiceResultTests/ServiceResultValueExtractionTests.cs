using BPITS.Results.Tests.Scaffolding;

namespace BPITS.Results.Tests.ServiceResultTests;

public class ServiceResultValueExtractionTests
{
    [Fact]
    public void TryGet_WithSuccessAndValue_ReturnsTrueAndValue()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "John", "john@example.com");
        var result = ServiceResult.Success(user);
        
        // Act
        var success = result.TryGet(out var extractedUser);
        
        // Assert
        Assert.True(success);
        Assert.NotNull(extractedUser);
        Assert.Equal(user, extractedUser);
    }
    
    [Fact]
    public void TryGet_WithSuccessAndNullValue_ReturnsFalseAndDefault()
    {
        // Arrange
        var result = ServiceResult.Success<User>(null);
        
        // Act
        var success = result.TryGet(out var extractedUser);
        
        // Assert
        Assert.False(success);
        Assert.Null(extractedUser);
    }
    
    [Fact]
    public void TryGet_WithFailure_ReturnsFalseAndDefault()
    {
        // Arrange
        var result = ServiceResult.Failure<User>("Not found", TestStatusCode.ResourceNotFound);
        
        // Act
        var success = result.TryGet(out var extractedUser);
        
        // Assert
        Assert.False(success);
        Assert.Null(extractedUser);
    }
    
    [Fact]
    public void Get_WithValue_ReturnsValue()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Jane", "jane@example.com");
        var result = ServiceResult.Success(user);
        
        // Act
        var extractedUser = result.Get();
        
        // Assert
        Assert.Equal(user, extractedUser);
    }
    
    [Fact]
    public void Get_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var result = ServiceResult.Success<User>(null);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => result.Get());
    }
    
    [Fact]
    public void Get_WithFailureAndNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var result = ServiceResult.Failure<User>("Failed");
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => result.Get());
    }
    
    [Fact]
    public void DirectValueAccess_WithSuccess_ReturnsValue()
    {
        // Arrange
        var school = new School(Guid.NewGuid(), "Test School", "123 Main St");
        var result = ServiceResult.Success(school);
        
        // Act
        var value = result.Value;
        
        // Assert
        Assert.Equal(school, value);
        Assert.True(result.IsSuccess);
    }
    
    [Fact]
    public void DirectValueAccess_WithFailure_ReturnsNullOrDefault()
    {
        // Arrange
        var result = ServiceResult.Failure<School>("Not found");
        
        // Act
        var value = result.Value;
        
        // Assert
        Assert.Null(value);
        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public void IsSuccess_ReflectsCorrectState()
    {
        // Arrange
        var successResult = ServiceResult.Success(42);
        var failureResult = ServiceResult.Failure<int>("Failed");
        
        // Assert
        Assert.True(successResult.IsSuccess);
        Assert.False(successResult.IsFailure);
        Assert.False(failureResult.IsSuccess);
        Assert.True(failureResult.IsFailure);
    }
    
    [Fact]
    public void RealWorldPattern_SafeExtraction_InController()
    {
        // Arrange - simulate service call
        var service = new MockUserService();
        service.AddUser(new User(Guid.NewGuid(), "Test User", "test@example.com"));
        var result = service.GetUsersPaged(0, 10);
        
        // Act - controller pattern from ROSCO
        ApiResult<PagedResult<UserDto>>? apiResult = null;
        if (result.TryGet(out var pagedResult))
        {
            // Transform each item in the paged result
            var dtoPagedResult = pagedResult.Select(user => user.ToDto());
            apiResult = ApiResult.Success(dtoPagedResult);
        }
        else
        {
            apiResult = ApiResult.FromServiceResult(result.MapValue<PagedResult<UserDto>>(_ => null));
        }
        
        // Assert
        Assert.NotNull(apiResult);
        Assert.Equal(TestStatusCode.Ok, apiResult.StatusCode);
        Assert.NotNull(apiResult.Value);
        Assert.Single(apiResult.Value.Items);
    }
    
    [Fact]
    public void ValueExtraction_WithComplexTypes_WorksCorrectly()
    {
        // Arrange
        var jobs = new List<Job>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now, 2),
            new(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now.AddDays(1), 3)
        };
        var pagedResult = new PagedResult<Job>(jobs, 2, 0, 10);
        var result = ServiceResult.Success(pagedResult);
        
        // Act
        var hasValue = result.TryGet(out var extracted);
        
        // Assert
        Assert.True(hasValue);
        Assert.NotNull(extracted);
        Assert.Equal(2, extracted.Items.Count);
        Assert.Equal(2, extracted.TotalCount);
    }
    
    [Fact]
    public void NullableValueTypes_ExtractCorrectly()
    {
        // Arrange
        int? nullableValue = 42;
        var resultWithValue = ServiceResult.Success(nullableValue);
        var resultWithNull = ServiceResult.Success<int?>(null);
        
        // Act
        var hasValue1 = resultWithValue.TryGet(out var value1);
        var hasValue2 = resultWithNull.TryGet(out var value2);
        
        // Assert
        Assert.True(hasValue1);
        Assert.Equal(42, value1);
        Assert.False(hasValue2);
        Assert.Null(value2);
    }
}