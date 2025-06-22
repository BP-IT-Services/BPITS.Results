using BPITS.Results.Tests.Scaffolding;

namespace BPITS.Results.Tests;

public class EdgeCaseTests
{
    [Fact]
    public void NullErrorMessage_HandledCorrectly()
    {
        // Act
        var result1 = ServiceResult.Failure<User>(null, TestStatusCode.GenericFailure);
        var result2 = ServiceResult.Failure<User>(exception: null, errorMessage: null);
        
        // Assert
        Assert.Null(result1.ErrorMessage);
        Assert.Equal(TestStatusCode.GenericFailure, result1.StatusCode);
        Assert.False(result1.IsSuccess);
        
        Assert.Null(result2.ErrorMessage);
        Assert.Equal(TestStatusCode.GenericFailure, result2.StatusCode);
    }
    
    [Fact]
    public void EmptyErrorDetails_HandledCorrectly()
    {
        // Arrange
        var emptyDetails = new Dictionary<string, string[]>();
        
        // Act
        var result = ServiceResult.ValidationFailure<User>("Failed", emptyDetails);
        
        // Assert
        Assert.NotNull(result.ErrorDetails);
        Assert.Empty(result.ErrorDetails);
        Assert.Equal(TestStatusCode.BadRequest, result.StatusCode);
    }
    
    [Fact]
    public void ErrorDetailsWithEmptyArrays_HandledCorrectly()
    {
        // Arrange
        var details = new Dictionary<string, string[]>
        {
            { "Field1", new string[] { } },
            { "Field2", new[] { "", "   " } }
        };
        
        // Act
        var result = ServiceResult.Failure<User>(null, "Failed", TestStatusCode.BadRequest, null, details);
        
        // Assert
        Assert.Equal(details, result.ErrorDetails);
        Assert.Empty(result.ErrorDetails["Field1"]);
        Assert.Equal(2, result.ErrorDetails["Field2"].Length);
    }
    
    [Fact]
    public void ChainedNullTransformations_HandleGracefully()
    {
        // Arrange
        ServiceResult<User?> nullUserResult = ServiceResult.Success<User?>(null);
        
        // Act
        var result = nullUserResult
            .MapValue(u => u?.ToDto())
            .MapValue(dto => dto?.Name)
            .MapValue(name => name?.ToUpper())
            .MapValue(upper => upper ?? "DEFAULT");
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("DEFAULT", result.Value);
    }
    
    [Fact]
    public void RecursiveTypeMapping_WorksCorrectly()
    {
        // Arrange
        var nestedResult = ServiceResult.Success(
            new { 
                User = new User(Guid.NewGuid(), "Test", "test@example.com"),
                Metadata = new { Created = DateTime.Now }
            }
        );
        
        // Act
        var mapped = nestedResult
            .MapValue(data => data?.User)
            .MapValue(user => user?.ToDto())
            .MapValue(dto => new { UserInfo = dto, Processed = true });
        
        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.NotNull(mapped.Value);
        Assert.True(mapped.Value.Processed);
        Assert.Equal("Test", mapped.Value.UserInfo?.Name);
    }
    
    [Fact]
    public void DefaultEnumValue_HandlesCorrectly()
    {
        // When status code is not specified, it should use the configured default
        // Act
        var result1 = ServiceResult.Failure<User>("Error");
        var result2 = ServiceResult.Failure("Error");
        
        // Assert
        Assert.Equal(TestStatusCode.GenericFailure, result1.StatusCode);
        Assert.Equal(TestStatusCode.GenericFailure, result2.StatusCode);
    }
    
    [Fact]
    public void LargeErrorDetails_HandledCorrectly()
    {
        // Arrange
        var largeErrors = new Dictionary<string, string[]>();
        for (int i = 0; i < 100; i++)
        {
            largeErrors[$"Field{i}"] = Enumerable.Range(0, 10)
                .Select(j => $"Error {j} for Field {i}")
                .ToArray();
        }
        
        // Act
        var result = ServiceResult.ValidationFailure<User>("Many errors", largeErrors);
        var apiResult = ApiResult.FromServiceResult(result);
        
        // Assert
        Assert.Equal(100, result.ErrorDetails?.Count);
        Assert.Equal(100, apiResult.ErrorDetails?.Count);
        Assert.All(result.ErrorDetails, kvp => Assert.Equal(10, kvp.Value.Length));
    }
    
    [Fact]
    public void SpecialCharactersInErrorMessages_PreservedCorrectly()
    {
        // Arrange
        var specialMessage = "Error: User's email \"test@example.com\" is invalid & contains special chars < > ";
        var specialErrors = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Contains 'quotes' and \"double quotes\"", "Has <angle> brackets" } }
        };
        
        // Act
        var result = ServiceResult.ValidationFailure<User>(specialMessage, specialErrors);
        var apiResult = ApiResult.FromServiceResult(result);
        
        // Assert
        Assert.Equal(specialMessage, result.ErrorMessage);
        Assert.Equal(specialMessage, apiResult.ErrorMessage);
        Assert.Contains("'quotes'", result.ErrorDetails?["Email"][0]);
        Assert.Contains("<angle>", apiResult.ErrorDetails?["Email"][1]);
    }
    
    [Fact]
    public void ValueTypeDefaults_HandledCorrectly()
    {
        // Test with various value types and their defaults
        
        // int
        var intResult = ServiceResult.Success<int>(0);
        Assert.True(intResult.TryGet(out var intValue));
        Assert.Equal(0, intValue);
        
        // bool
        var boolResult = ServiceResult.Success<bool>(false);
        Assert.True(boolResult.TryGet(out var boolValue));
        Assert.False(boolValue);
        
        // DateTime
        var dateResult = ServiceResult.Success<DateTime>(default);
        Assert.True(dateResult.TryGet(out var dateValue));
        Assert.Equal(default(DateTime), dateValue);
        
        // Guid
        var guidResult = ServiceResult.Success<Guid>(Guid.Empty);
        Assert.True(guidResult.TryGet(out var guidValue));
        Assert.Equal(Guid.Empty, guidValue);
    }
    
    [Fact]
    public void ExtremelyLongErrorMessages_HandledCorrectly()
    {
        // Arrange
        var longMessage = string.Join(" ", Enumerable.Repeat("This is a very long error message.", 100));
        
        // Act
        var result = ServiceResult.Failure<User>(longMessage);
        var apiResult = ApiResult.FromServiceResult(result);
        
        // Assert
        Assert.Equal(longMessage, result.ErrorMessage);
        Assert.Equal(longMessage, apiResult.ErrorMessage);
        Assert.True(result.ErrorMessage.Length > 3000);
    }
    
    [Fact]
    public void CircularReferenceScenario_HandledGracefully()
    {
        // This tests that the result pattern doesn't break with circular references
        var user1 = new User(Guid.NewGuid(), "User1", "user1@test.com");
        var circularRef = new 
        {
            User = user1,
            Self = (object?)null
        };
        // Create circular reference
        circularRef = new { User = user1, Self = (object?)circularRef };
        
        // Act
        var result = ServiceResult.Success(circularRef);
        var apiResult = ApiResult.FromServiceResult(result);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(apiResult.StatusCode == TestStatusCode.Ok);
    }
}