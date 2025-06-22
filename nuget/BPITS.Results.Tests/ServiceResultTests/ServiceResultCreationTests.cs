using BPITS.Results.Tests.Scaffolding;

namespace BPITS.Results.Tests.ServiceResultTests;

public class ServiceResultCreationTests
{
    [Fact]
    public void Success_WithNoValue_CreatesExpectedResult()
    {
        // Act
        var result = Results.ServiceResult.Success();
        
        // Assert
        Assert.Equal(TestStatusCode.Ok, result.StatusCode);
        Assert.Null(result.Value);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorDetails);
        Assert.Null(result.InnerException);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }
    
    [Fact]
    public void Success_WithValue_CreatesExpectedResult()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "John Doe", "john@example.com");
        
        // Act
        var result = Results.ServiceResult.Success(user);
        
        // Assert
        Assert.Equal(TestStatusCode.Ok, result.StatusCode);
        Assert.Equal(user, result.Value);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorDetails);
        Assert.Null(result.InnerException);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }
    
    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessResult()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Jane Doe", "jane@example.com");
        
        // Act
        ServiceResult<User> result = user;
        
        // Assert
        Assert.Equal(TestStatusCode.Ok, result.StatusCode);
        Assert.Equal(user, result.Value);
        Assert.True(result.IsSuccess);
    }
    
    [Fact]
    public void ImplicitConversion_FromNull_CreatesSuccessResultWithNullValue()
    {
        // Act
        User? nullUser = null;
        ServiceResult<User> result = nullUser;
        
        // Assert
        Assert.Equal(TestStatusCode.Ok, result.StatusCode);
        Assert.Null(result.Value);
        Assert.True(result.IsSuccess);
    }
    
    [Fact]
    public void Failure_WithMessageOnly_UsesDefaultStatusCode()
    {
        // Act
        var result = Results.ServiceResult.Failure<User>("Something went wrong");
        
        // Assert
        Assert.Equal(TestStatusCode.GenericFailure, result.StatusCode); // Uses DefaultFailureValue
        Assert.Equal("Something went wrong", result.ErrorMessage);
        Assert.Null(result.Value);
        Assert.Null(result.InnerException);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }
    
    [Fact]
    public void Failure_WithMessageAndStatusCode_CreatesExpectedResult()
    {
        // Act
        var result = Results.ServiceResult.Failure<User>("User not found", TestStatusCode.ResourceNotFound);
        
        // Assert
        Assert.Equal(TestStatusCode.ResourceNotFound, result.StatusCode);
        Assert.Equal("User not found", result.ErrorMessage);
        Assert.Null(result.Value);
        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public void Failure_WithException_CapturesExceptionDetails()
    {
        // Arrange
        var exception = new TestDatabaseException("Database connection failed");
        
        // Act
        var result = Results.ServiceResult.Failure<User>(exception, "Failed to retrieve user");
        
        // Assert
        Assert.Equal(TestStatusCode.GenericFailure, result.StatusCode);
        Assert.Equal("Failed to retrieve user", result.ErrorMessage);
        Assert.Equal(exception, result.InnerException);
        Assert.Null(result.Value);
        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public void Failure_WithAllParameters_CreatesCompleteResult()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid state");
        var errorDetails = new Dictionary<string, string[]>
        {
            { "Field1", new[] { "Error 1", "Error 2" } }
        };
        
        // Act
        var result = Results.ServiceResult.Failure<User>(
            exception,
            "Operation failed",
            TestStatusCode.BadRequest,
            null,
            errorDetails
        );
        
        // Assert
        Assert.Equal(TestStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Operation failed", result.ErrorMessage);
        Assert.Equal(exception, result.InnerException);
        Assert.Equal(errorDetails, result.ErrorDetails);
        Assert.Null(result.Value);
    }
    
    [Fact]
    public void ValidationFailure_WithMessage_UsesBadRequestStatusCode()
    {
        // Act
        var result = Results.ServiceResult.ValidationFailure<User>("Validation failed");
        
        // Assert
        Assert.Equal(TestStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Validation failed", result.ErrorMessage);
        Assert.Null(result.Value);
        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public void ValidationFailure_WithFieldError_CreatesDetailedError()
    {
        // Act
        var result = Results.ServiceResult.ValidationFailure<User>("Email", "Email is required");
        
        // Assert
        Assert.Equal(TestStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Email is required", result.ErrorMessage);
        Assert.NotNull(result.ErrorDetails);
        Assert.Single(result.ErrorDetails);
        Assert.Contains("Email", result.ErrorDetails.Keys);
        Assert.Contains("Email is required", result.ErrorDetails["Email"]);
    }
    
    [Fact]
    public void ValidationFailure_WithMultipleErrors_CreatesDetailedErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Email is required", "Email format is invalid" } },
            { "Name", new[] { "Name is too short" } }
        };
        
        // Act
        var result = Results.ServiceResult.ValidationFailure<User>("Multiple validation errors", errors);
        
        // Assert
        Assert.Equal(TestStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Multiple validation errors", result.ErrorMessage);
        Assert.Equal(errors, result.ErrorDetails);
        Assert.Equal(2, result.ErrorDetails.Count);
    }
    
    [Fact]
    public void NonGenericSuccess_CreatesExpectedResult()
    {
        // Act
        var result = Results.ServiceResult.Success();
        
        // Assert
        Assert.Equal(TestStatusCode.Ok, result.StatusCode);
        Assert.Null(result.Value);
        Assert.True(result.IsSuccess);
    }
    
    [Fact]
    public void NonGenericFailure_WithMessage_CreatesExpectedResult()
    {
        // Act
        var result = Results.ServiceResult.Failure("Operation failed");
        
        // Assert
        Assert.Equal(TestStatusCode.GenericFailure, result.StatusCode);
        Assert.Equal("Operation failed", result.ErrorMessage);
        Assert.Null(result.Value);
        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public void ImplicitConversion_BetweenGenericAndNonGeneric_Works()
    {
        // Arrange
        var genericResult = Results.ServiceResult.Success<object>(new { Id = 1 });
        
        // Act
        Results.ServiceResult nonGenericResult = genericResult;
        ServiceResult<object> backToGeneric = nonGenericResult;
        
        // Assert
        Assert.Equal(genericResult.StatusCode, nonGenericResult.StatusCode);
        Assert.Equal(genericResult.Value, nonGenericResult.Value);
        Assert.Equal(genericResult.ErrorMessage, nonGenericResult.ErrorMessage);
        Assert.Equal(backToGeneric.StatusCode, genericResult.StatusCode);
    }
}