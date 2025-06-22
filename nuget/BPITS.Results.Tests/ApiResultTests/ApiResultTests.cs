using BPITS.Results.Tests.Scaffolding;

namespace BPITS.Results.Tests.ApiResultTests;

public class ApiResultTests
{
    [Fact]
    public void Success_WithNoValue_CreatesExpectedResult()
    {
        // Act
        var result = ApiResult.Success();

        // Assert
        Assert.Equal(TestStatusCode.Ok, result.StatusCode);
        Assert.Null(result.Value);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Success_WithValue_CreatesExpectedResult()
    {
        // Arrange
        var userDto = new UserDto(Guid.NewGuid(), "John", "john@example.com");

        // Act
        var result = ApiResult.Success(userDto);

        // Assert
        Assert.Equal(TestStatusCode.Ok, result.StatusCode);
        Assert.Equal(userDto, result.Value);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessResult()
    {
        // Arrange
        var schoolDto = new SchoolDto(Guid.NewGuid(), "Test School");

        // Act
        ApiResult<SchoolDto> result = schoolDto;

        // Assert
        Assert.Equal(TestStatusCode.Ok, result.StatusCode);
        Assert.Equal(schoolDto, result.Value);
    }

    [Fact]
    public void Failure_WithMessage_CreatesExpectedResult()
    {
        // Act
        var result = ApiResult.Failure<UserDto>(
            "User not found",
            TestStatusCode.ResourceNotFound
        );

        // Assert
        Assert.Equal(TestStatusCode.ResourceNotFound, result.StatusCode);
        Assert.Equal("User not found", result.ErrorMessage);
        Assert.Null(result.Value);
    }

    [Fact]
    public void ApiResult_DoesNotExposeException()
    {
        // This test verifies that ApiResult doesn't have an InnerException property
        // by checking the type's properties
        var apiResultType = typeof(ApiResult<User>);
        var hasInnerException = apiResultType.GetProperty("InnerException") != null;

        Assert.False(hasInnerException, "ApiResult should not expose InnerException property");
    }

    [Fact]
    public void FromServiceResult_RemovesExceptionDetails()
    {
        // Arrange
        var exception = new TestDatabaseException("Sensitive DB error details");
        var serviceResult = ServiceResult.Failure<User>(
            exception,
            "Failed to retrieve user",
            TestStatusCode.GenericFailure
        );

        // Act
        var apiResult = ApiResult.FromServiceResult(serviceResult);

        // Assert
        Assert.Equal(TestStatusCode.GenericFailure, apiResult.StatusCode);
        Assert.Equal("Failed to retrieve user", apiResult.ErrorMessage);
        Assert.Null(apiResult.Value);
        // Exception details are not exposed in ApiResult
    }

    [Fact]
    public void FromServiceResult_PreservesErrorDetails()
    {
        // Arrange
        var errorDetails = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Invalid format", "Already exists" } },
            { "Name", new[] { "Too short" } }
        };

        var serviceResult = ServiceResult.ValidationFailure<User>(
            "Validation failed",
            errorDetails
        );

        // Act
        var apiResult = ApiResult.FromServiceResult(serviceResult);

        // Assert
        Assert.Equal(TestStatusCode.BadRequest, apiResult.StatusCode);
        Assert.Equal("Validation failed", apiResult.ErrorMessage);
        Assert.Equal(errorDetails, apiResult.ErrorDetails);
    }

    [Fact]
    public void ImplicitConversion_FromServiceResult_Works()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Test", "test@example.com");
        ServiceResult<User> serviceResult = user;

        // Act
        ApiResult<User> apiResult = serviceResult;

        // Assert
        Assert.Equal(serviceResult.StatusCode, apiResult.StatusCode);
        Assert.Equal(serviceResult.Value, apiResult.Value);
        Assert.Equal(serviceResult.ErrorMessage, apiResult.ErrorMessage);
    }

    [Fact]
    public void FromServiceResult_WithTypeTransformation_Works()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "John", "john@example.com");
        var serviceResult = ServiceResult.Success(user);

        // Act
        var apiResult = ApiResult.FromServiceResult(
            serviceResult.MapValue(u => u?.ToDto())
        );

        // Assert
        Assert.Equal(TestStatusCode.Ok, apiResult.StatusCode);
        Assert.NotNull(apiResult.Value);
        Assert.IsType<UserDto>(apiResult.Value);
    }

    [Fact]
    public void FromServiceResult_WithOverrides_AppliesNewValues()
    {
        // Arrange
        var serviceResult = ServiceResult.Failure<User>(
            "Internal error",
            TestStatusCode.GenericFailure
        );

        // Act
        var apiResult = ApiResult.FromServiceResult(
            serviceResult,
            errorMessage: "An error occurred",
            statusCode: TestStatusCode.BadRequest
        );

        // Assert
        Assert.Equal(TestStatusCode.BadRequest, apiResult.StatusCode);
        Assert.Equal("An error occurred", apiResult.ErrorMessage);
    }

    [Fact]
    public void NonGenericConversions_Work()
    {
        // Arrange
        var genericApiResult = ApiResult.Success<object>(new { Id = 1 });
        var serviceResult = ServiceResult.Success();

        // Act
        ApiResult nonGeneric = genericApiResult;
        ApiResult fromService = serviceResult;

        // Assert
        Assert.Equal(genericApiResult.StatusCode, nonGeneric.StatusCode);
        Assert.Equal(genericApiResult.Value, nonGeneric.Value);
        Assert.Equal(serviceResult.StatusCode, fromService.StatusCode);
    }

    [Fact]
    public async void RealWorldPattern_ControllerAction()
    {
        // Arrange - Simulate a controller action like in ROSCO
        var mockService = new MockUserService();
        var createRequest = new CreateUserRequest("John Doe", "john@example.com", "password123");

        async Task<ApiResult<UserDto>> CreateUserAction(CreateUserRequest request)
        {
            try
            {
                var createResult = mockService.CreateUser(request);
                return ApiResult.FromServiceResult(createResult.MapValue(u => u?.ToDto()));
            }
            catch (Exception)
            {
                return ApiResult.Failure<UserDto>(
                    "An error occurred while creating the user",
                    TestStatusCode.GenericFailure
                );
            }
        }

        // Act
        var result = await CreateUserAction(createRequest);

        // Assert
        Assert.True(result.StatusCode == TestStatusCode.Ok);
        Assert.NotNull(result.Value);
        Assert.Equal("John Doe", result.Value.Name);
    }

    [Fact]
    public void RealWorldPattern_ValidationErrors()
    {
        // Arrange
        var mockService = new MockUserService();
        var invalidRequest = new CreateUserRequest("", "", ""); // Invalid request

        // Act
        var serviceResult = mockService.CreateUser(invalidRequest);
        var apiResult = ApiResult.FromServiceResult(serviceResult.MapValue(u => u?.ToDto()));

        // Assert
        Assert.Equal(TestStatusCode.BadRequest, apiResult.StatusCode);
        Assert.Equal("Email is required", apiResult.ErrorMessage);
        Assert.NotNull(apiResult.ErrorDetails);
        Assert.Contains("Email", apiResult.ErrorDetails.Keys);
    }
}