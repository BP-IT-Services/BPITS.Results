using System.Net;
using BPITS.Results.Abstractions;

namespace BPITS.Results.Tests.Scaffolding;

// Test status code enum similar to ROSCO's ResultStatusCode
[GenerateApiResult(
    DefaultFailureValue = nameof(GenericFailure),
    BadRequestValue = nameof(BadRequest)
)]
[GenerateServiceResult(
    DefaultFailureValue = nameof(GenericFailure),
    BadRequestValue = nameof(BadRequest)
)]
public enum TestStatusCode
{
    GenericFailure = 0,
    Ok = 1,
    BadRequest = 2,
    InsufficientPermissions = 3,
    AuthenticationTokenInvalid = 4,
    InvalidCredentials = 5,
    ResourceNotFound = 6,
    ResourceAlreadyExists = 7,
    ResourceExpired = 8,
    ResourceDisabled = 9,
    FunctionalityDisabled = 10,
    ExternalServiceFailure = 11,
}

// Domain models
public record User(Guid Id, string Name, string Email)
{
    public UserDto ToDto() => new(Id, Name, Email);
}

public record UserDto(Guid Id, string Name, string Email);

public record CreateUserRequest(string Name, string Email, string Password);

public record School(Guid Id, string Name, string Address)
{
    public SchoolDto ToDto() => new(Id, Name);
}

public record SchoolDto(Guid Id, string Name);

public record Job(Guid Id, Guid SchoolId, DateTime ShootDate, int RequiredPhotographers)
{
    public JobDto ToDto() => new(Id, SchoolId, ShootDate.ToString("yyyy-MM-dd"));
}

public record JobDto(Guid Id, Guid SchoolId, string ShootDate);

// Paged result for testing pagination scenarios
public record PagedResult<T>(List<T> Items, int TotalCount, int PageIndex, int PageSize)
{
    public PagedResult<TOut> Select<TOut>(Func<T, TOut> selector)
    {
        return new PagedResult<TOut>(
            Items.Select(selector).ToList(),
            TotalCount,
            PageIndex,
            PageSize
        );
    }
}

// Mock service for testing service patterns
public class MockUserService
{
    private readonly Dictionary<Guid, User> _users = new();

    public ServiceResult<User> GetUser(Guid id)
    {
        if (_users.TryGetValue(id, out var user))
            return ServiceResult.Success(user);
        
        return ServiceResult.Failure<User>("User not found", TestStatusCode.ResourceNotFound);
    }

    public ServiceResult<User> CreateUser(CreateUserRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return ServiceResult.ValidationFailure<User>(
                nameof(request.Email), 
                "Email is required"
            );
        }

        if (_users.Values.Any(u => u.Email == request.Email))
        {
            return ServiceResult.ValidationFailure<User>(
                "Email address already exists",
                new Dictionary<string, string[]>
                {
                    { "Email", new[] { "Email address is already in use" } }
                }
            );
        }

        var user = new User(Guid.NewGuid(), request.Name, request.Email);
        _users[user.Id] = user;
        return user; // Implicit conversion
    }

    public ServiceResult<PagedResult<User>> GetUsersPaged(int pageIndex, int pageSize)
    {
        var users = _users.Values.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        var result = new PagedResult<User>(users, _users.Count, pageIndex, pageSize);
        return ServiceResult.Success(result);
    }

    public void AddUser(User user) => _users[user.Id] = user;
}

// Test exception
public class TestDatabaseException : Exception
{
    public TestDatabaseException(string message) : base(message) { }
}