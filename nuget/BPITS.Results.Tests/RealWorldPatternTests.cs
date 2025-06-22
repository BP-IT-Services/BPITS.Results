using BPITS.Results.Tests.Scaffolding;

namespace BPITS.Results.Tests;

public class RealWorldPatternTests
{
    [Fact]
    public async Task ServiceChaining_WithMultipleValidations_PropagatesErrors()
    {
        // Arrange - Similar to ROSCO's JobService.CreateJobAsync pattern
        var mockSchoolService = new MockSchoolService();
        var mockWorkflowService = new MockWorkflowService();

        async Task<ServiceResult<Job>> CreateJobWithDependencies(Guid schoolId, int requiredPhotographers)
        {
            // Validate school exists
            var getSchoolResult = await mockSchoolService.GetSchoolAsync(schoolId, true);
            if (getSchoolResult.IsFailure && getSchoolResult.StatusCode != TestStatusCode.ResourceNotFound)
                return getSchoolResult.PassThroughFail<Job>();

            if (getSchoolResult.IsFailure)
                return ServiceResult.ValidationFailure<Job>("SchoolId", "The specified school could not be found.");

            // Create job
            var job = new Job(Guid.NewGuid(), schoolId, DateTime.Now, requiredPhotographers);

            // Create workflow
            var createWorkflowResult = await mockWorkflowService.CreateWorkflowFromJob(job);
            if (createWorkflowResult.IsSuccess)
                return job; // Implicit conversion

            // Rollback on failure
            return createWorkflowResult.PassThroughFail<Job>();
        }

        // Act
        var result = await CreateJobWithDependencies(Guid.NewGuid(), 2);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TestStatusCode.BadRequest, result.StatusCode);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("SchoolId", result.ErrorDetails.Keys);
    }

    [Fact]
    public async Task PagedResultHandling_TransformsCorrectly()
    {
        // Arrange - Similar to ROSCO's controller patterns
        var service = new MockUserService();
        service.AddUser(new User(Guid.NewGuid(), "User1", "user1@test.com"));
        service.AddUser(new User(Guid.NewGuid(), "User2", "user2@test.com"));

        async Task<ApiResult<PagedResult<UserDto>>> GetUsersPagedAction(int pageIndex, int pageSize)
        {
            var result = service.GetUsersPaged(pageIndex, pageSize);
            if (result.TryGet(out var pagedResult))
            {
                return pagedResult.Select(user => user.ToDto());
            }

            return ApiResult.FromServiceResult(result.MapValue<PagedResult<UserDto>>(_ => null));
        }

        // Act
        var apiResult = await GetUsersPagedAction(0, 10);

        // Assert
        Assert.True(apiResult.StatusCode == TestStatusCode.Ok);
        Assert.NotNull(apiResult.Value);
        Assert.Equal(2, apiResult.Value.Items.Count);
        Assert.All(apiResult.Value.Items, dto => Assert.IsType<UserDto>(dto));
    }

    [Fact]
    public void ComplexValidation_WithMultipleFields_HandlesCorrectly()
    {
        // Arrange - Similar to ROSCO's validation patterns
        ServiceResult<User> ValidateAndCreateUser(string name, string email, string password)
        {
            var validationErrors = new Dictionary<string, string[]>();

            if (string.IsNullOrEmpty(name))
                validationErrors[nameof(name)] = new[] { "Name is required" };

            if (string.IsNullOrEmpty(email))
                validationErrors[nameof(email)] = new[] { "Email is required" };
            else if (!email.Contains("@"))
                validationErrors[nameof(email)] = new[] { "Email format is invalid" };

            if (string.IsNullOrEmpty(password))
                validationErrors[nameof(password)] = new[] { "Password is required" };
            else if (password.Length < 8)
                validationErrors[nameof(password)] = new[] { "Password must be at least 8 characters" };

            if (validationErrors.Any())
            {
                return ServiceResult.ValidationFailure<User>(
                    "Validation failed",
                    validationErrors
                );
            }

            var user = new User(Guid.NewGuid(), name, email);
            return ServiceResult.Success(user);
        }

        // Act
        var result = ValidateAndCreateUser("", "invalid-email", "123");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TestStatusCode.BadRequest, result.StatusCode);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal(3, result.ErrorDetails.Count);
        Assert.Contains("name", result.ErrorDetails.Keys);
        Assert.Contains("email", result.ErrorDetails.Keys);
        Assert.Contains("password", result.ErrorDetails.Keys);
    }

    [Fact]
    public void CalendarService_Pattern_WithConditionalMapping()
    {
        // Arrange - Similar to ROSCO's CalendarService patterns
        var jobSheetSettings = new { ShowPhotographers = false, ShowSchoolName = true };

        ServiceResult<string> GetJobLabel(Job job, bool obeySettings)
        {
            if (!obeySettings)
            {
                return $"{job.Id} x{job.RequiredPhotographers}";
            }

            var label = jobSheetSettings.ShowSchoolName ? job.Id.ToString() : "Hidden";
            if (jobSheetSettings.ShowPhotographers)
                label += $" x{job.RequiredPhotographers}";

            return label;
        }

        // Act
        var job = new Job(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now, 3);
        var result1 = GetJobLabel(job, false);
        var result2 = GetJobLabel(job, true);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.Contains("x3", result1.Value);

        Assert.True(result2.IsSuccess);
        Assert.DoesNotContain("x3", result2.Value);
    }

    [Fact]
    public async Task ExceptionHandling_InControllers_SanitizesErrors()
    {
        // Arrange - Controller pattern with exception handling
        async Task<ApiResult<UserDto>> GetUserWithExceptionHandling(Guid userId)
        {
            try
            {
                if (userId == Guid.Empty)
                    throw new ArgumentException("Invalid user ID");

                var service = new MockUserService();
                var result = service.GetUser(userId);
                return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
            }
            catch (Exception ex)
            {
                // Log exception (not shown)
                return ApiResult.Failure<UserDto>(
                    "An error occurred while retrieving the user",
                    TestStatusCode.GenericFailure
                );
            }
        }

        // Act
        var result = await GetUserWithExceptionHandling(Guid.Empty);

        // Assert
        Assert.False(result.StatusCode == TestStatusCode.Ok);
        Assert.Equal(TestStatusCode.GenericFailure, result.StatusCode);
        Assert.Equal("An error occurred while retrieving the user", result.ErrorMessage);
        // Exception details are not exposed
    }

    [Fact]
    public void AuthorizationPattern_WithClaimsValidation()
    {
        // Arrange - Similar to ROSCO's auth patterns
        ServiceResult<Job> GetJobWithAuth(Guid jobId, Guid? userPhotographerId)
        {
            var job = new Job(jobId, Guid.NewGuid(), DateTime.Now, 2);

            // Simulate photographer authorization check
            var authorizedPhotographers = new[] { Guid.NewGuid(), Guid.NewGuid() };

            if (userPhotographerId.HasValue && !authorizedPhotographers.Contains(userPhotographerId.Value))
            {
                return ServiceResult.Failure<Job>(
                    "Insufficient permissions",
                    TestStatusCode.InsufficientPermissions
                );
            }

            return job;
        }

        // Act
        var unauthorizedResult = GetJobWithAuth(Guid.NewGuid(), Guid.NewGuid());
        var authorizedResult = GetJobWithAuth(Guid.NewGuid(), null);

        // Assert
        Assert.False(unauthorizedResult.IsSuccess);
        Assert.Equal(TestStatusCode.InsufficientPermissions, unauthorizedResult.StatusCode);

        Assert.True(authorizedResult.IsSuccess);
        Assert.NotNull(authorizedResult.Value);
    }
}

// Mock services for testing
public class MockSchoolService
{
    public async Task<ServiceResult<School>> GetSchoolAsync(Guid schoolId, bool forceNotFound = false)
    {
        if (forceNotFound)
        {
            return ServiceResult.Failure<School>(
                "School not found",
                TestStatusCode.ResourceNotFound
            );
        }
        
        if (schoolId == Guid.Empty)
        {
            return ServiceResult.Failure<School>(
                "Invalid school ID",
                TestStatusCode.BadRequest
            );
        }

        var school = new School(schoolId, "Test School", "123 Main St");
        return school; // Implicit conversion to ServiceResult.Success<School>(...)
    }
}

public class MockWorkflowService
{
    public async Task<ServiceResult<object>> CreateWorkflowFromJob(Job job)
    {
        if (job.RequiredPhotographers > 10)
        {
            return ServiceResult.Failure<object>(
                "Too many photographers required",
                TestStatusCode.BadRequest
            );
        }

        return new { WorkflowId = Guid.NewGuid() }; // Implicit conversion to ServiceResult.Success<object>(...)
    }
}