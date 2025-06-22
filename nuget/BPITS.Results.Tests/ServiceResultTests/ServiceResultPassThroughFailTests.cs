using BPITS.Results.Tests.Scaffolding;

namespace BPITS.Results.Tests;

public class ServiceResultPassThroughFailTests
{
    [Fact]
    public void PassThroughFail_Generic_PreservesErrorInfo()
    {
        // Arrange
        var originalResult = ServiceResult.Failure<User>(
            "User not found", 
            TestStatusCode.ResourceNotFound
        );
        
        // Act
        var passthroughResult = originalResult.PassThroughFail<UserDto>();
        
        // Assert
        Assert.Equal(TestStatusCode.ResourceNotFound, passthroughResult.StatusCode);
        Assert.Equal("User not found", passthroughResult.ErrorMessage);
        Assert.Null(passthroughResult.Value);
        Assert.False(passthroughResult.IsSuccess);
    }
    
    [Fact]
    public void PassThroughFail_WithOverrides_AppliesNewValues()
    {
        // Arrange
        var originalResult = ServiceResult.Failure<User>(
            "Original error", 
            TestStatusCode.GenericFailure
        );
        
        // Act
        var passthroughResult = originalResult.PassThroughFail<UserDto>(
            null,
            "New error message",
            TestStatusCode.BadRequest
        );
        
        // Assert
        Assert.Equal(TestStatusCode.BadRequest, passthroughResult.StatusCode);
        Assert.Equal("New error message", passthroughResult.ErrorMessage);
    }
    
    [Fact]
    public void PassThroughFail_WithValue_SetsNewValue()
    {
        // Arrange
        var originalResult = ServiceResult.Failure<User>("Failed", TestStatusCode.BadRequest);
        var defaultDto = new UserDto(Guid.Empty, "Default", "default@example.com");
        
        // Act
        var passthroughResult = originalResult.PassThroughFail(
            value: defaultDto,
            errorMessage: "Fallback to default"
        );
        
        // Assert
        Assert.Equal(defaultDto, passthroughResult.Value);
        Assert.Equal("Fallback to default", passthroughResult.ErrorMessage);
        Assert.Equal(TestStatusCode.BadRequest, passthroughResult.StatusCode);
    }
    
    [Fact]
    public void PassThroughFail_PreservesExceptionAndErrorDetails()
    {
        // Arrange
        var exception = new TestDatabaseException("DB Error");
        var errorDetails = new Dictionary<string, string[]>
        {
            { "Field1", new[] { "Error1" } }
        };
        
        var originalResult = ServiceResult.Failure<User>(
            exception,
            "Database error",
            TestStatusCode.ExternalServiceFailure,
            null,
            errorDetails
        );
        
        // Act
        var passthroughResult = originalResult.PassThroughFail<School>();
        
        // Assert
        Assert.Equal(exception, passthroughResult.InnerException);
        Assert.Equal(errorDetails, passthroughResult.ErrorDetails);
        Assert.Equal(TestStatusCode.ExternalServiceFailure, passthroughResult.StatusCode);
        Assert.Equal("Database error", passthroughResult.ErrorMessage);
    }
    
    [Fact]
    public void PassThroughFail_NonGenericToGeneric_Works()
    {
        // Arrange
        var nonGenericResult = ServiceResult.Failure(
            "Operation failed",
            TestStatusCode.BadRequest
        );
        
        // Act
        var genericResult = nonGenericResult.PassThroughFail<Job>();
        
        // Assert
        Assert.Equal(TestStatusCode.BadRequest, genericResult.StatusCode);
        Assert.Equal("Operation failed", genericResult.ErrorMessage);
        Assert.Null(genericResult.Value);
    }
    
    [Fact]
    public void PassThroughFail_GenericToNonGeneric_Works()
    {
        // Arrange
        var genericResult = ServiceResult.Failure<User>(
            "User operation failed",
            TestStatusCode.InsufficientPermissions
        );
        
        // Act
        var nonGenericResult = genericResult.PassThroughFail();
        
        // Assert
        Assert.Equal(TestStatusCode.InsufficientPermissions, nonGenericResult.StatusCode);
        Assert.Equal("User operation failed", nonGenericResult.ErrorMessage);
        Assert.Null(nonGenericResult.Value);
    }
    
    [Fact]
    public async void RealWorldScenario_ErrorPropagationChain()
    {
        // Arrange - Simulate ROSCO's JobService pattern
        async Task<ServiceResult<Job>> CreateJobWithValidations(Guid schoolId)
        {
            // Step 1: Get school
            var schoolResult = await GetSchoolAsync(schoolId);
            if (!schoolResult.TryGet(out var school))
            {
                return schoolResult.PassThroughFail<Job>();
            }
            
            // Step 2: Validate school is active
            if (school.Name.Contains("Inactive"))
            {
                return ServiceResult.Failure<Job>(
                    "School is not active",
                    TestStatusCode.BadRequest
                );
            }
            
            // Step 3: Create job
            var job = new Job(Guid.NewGuid(), schoolId, DateTime.Now, 2);
            return ServiceResult.Success(job);
        }
        
        async Task<ServiceResult<School>> GetSchoolAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return ServiceResult.Failure<School>(
                    "Invalid school ID",
                    TestStatusCode.BadRequest
                );
            }
            
            var school = new School(id, "Inactive School", "123 Main St");
            return school; // Implicit casting to ServiceResult.Success<School>(...)
        }
        
        // Act
        var result = await CreateJobWithValidations(Guid.Empty);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TestStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Invalid school ID", result.ErrorMessage);
        Assert.Null(result.Value);
    }
    
    [Fact]
    public void PassThroughFail_WithPartialOverrides_MergesCorrectly()
    {
        // Arrange
        var errorDetails = new Dictionary<string, string[]>
        {
            { "Original", new[] { "Original error" } }
        };
        
        var newErrorDetails = new Dictionary<string, string[]>
        {
            { "New", new[] { "New error" } }
        };
        
        var originalResult = ServiceResult.Failure<User>(
            null,
            "Original message",
            TestStatusCode.BadRequest,
            null,
            errorDetails
        );
        
        // Act
        var result1 = originalResult.PassThroughFail<UserDto>(
            errorMessage: "New message"  // Only override message
        );
        
        var result2 = originalResult.PassThroughFail<UserDto>(
            statusCode: TestStatusCode.GenericFailure  // Only override status
        );
        
        var result3 = originalResult.PassThroughFail<UserDto>(
            errorDetails: newErrorDetails  // Only override error details
        );
        
        // Assert
        Assert.Equal("New message", result1.ErrorMessage);
        Assert.Equal(TestStatusCode.BadRequest, result1.StatusCode);
        Assert.Equal(errorDetails, result1.ErrorDetails);
        
        Assert.Equal("Original message", result2.ErrorMessage);
        Assert.Equal(TestStatusCode.GenericFailure, result2.StatusCode);
        
        Assert.Equal(newErrorDetails, result3.ErrorDetails);
        Assert.Equal("Original message", result3.ErrorMessage);
    }
    
    [Fact]
    public void FailureFromServiceResult_CreatesNewFailure()
    {
        // Arrange
        var sourceResult = ServiceResult.Failure<User>(
            "Source error",
            TestStatusCode.ResourceNotFound
        );
        
        // Act
        // Explicitly specify generic parameter to resolve ambiguity
        var newResult = ServiceResult.FailureFromServiceResult<User>(
            sourceResult,
            "Override error"
        );
        
        // Assert
        Assert.Equal("Override error", newResult.ErrorMessage);
        Assert.Equal(TestStatusCode.ResourceNotFound, newResult.StatusCode);
        Assert.Null(newResult.Value);
    }
    
    [Fact]
    public void FailureFromServiceResult_WithNewType_CreatesTypedFailure()
    {
        // Arrange
        var sourceResult = ServiceResult.Failure<User>(
            "Source error",
            TestStatusCode.ResourceNotFound
        );
        
        // Act
        // Explicitly specify both generic parameters for typed result
        var newResult = ServiceResult.FailureFromServiceResult<User, School>(
            sourceResult,
            value: null,
            errorMessage: "Override error"
        );
        
        // Assert
        Assert.Equal("Override error", newResult.ErrorMessage);
        Assert.Equal(TestStatusCode.ResourceNotFound, newResult.StatusCode);
        Assert.Null(newResult.Value);
        Assert.IsType<ServiceResult<School>>(newResult);
    }
}