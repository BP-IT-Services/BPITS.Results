namespace BPITS.Results.AspNetCore.Tests.Scaffolding;

public record User(Guid Id, string Name, string Email)
{
    public UserDto ToDto() => new(Id, Name, Email);
}

public record UserDto(Guid Id, string Name, string Email);

public record CreateUserRequest(string? Name, string? Email);

/// <summary>
/// Stands in for an application service so endpoints follow the documented
/// ServiceResult -> ApiResult flow rather than constructing ApiResults directly.
/// </summary>
public class UserService
{
    public static readonly Guid ExistingUserId = Guid.Parse("8a6f2f39-6f4e-4a2e-9a52-0f1c4b8f7d11");

    private readonly Dictionary<Guid, User> _users = new()
    {
        [ExistingUserId] = new User(ExistingUserId, "Ada Lovelace", "ada@example.com"),
    };

    public Task<ServiceResult<User>> GetUserAsync(Guid id)
    {
        var result = _users.TryGetValue(id, out var user)
            ? ServiceResult.Success(user)
            : ServiceResult.Failure<User>("User not found", TestStatusCode.ResourceNotFound);

        return Task.FromResult(result);
    }

    public Task<ServiceResult<User>> CreateUserAsync(CreateUserRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors[nameof(request.Name)] = new[] { "Name is required." };

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            errors[nameof(request.Email)] = new[] { "Email is required.", "Email must be a valid address." };

        if (errors.Count > 0)
            return Task.FromResult(ServiceResult.ValidationFailure<User>("Validation failed", errors));

        var user = new User(Guid.NewGuid(), request.Name!, request.Email!);
        _users[user.Id] = user;
        return Task.FromResult(ServiceResult.Success(user));
    }

    public Task<ServiceResult> DeleteUserAsync(Guid id)
    {
        var result = _users.Remove(id)
            ? ServiceResult.Success()
            : ServiceResult.Failure("User not found", TestStatusCode.ResourceNotFound);

        return Task.FromResult(result);
    }
}
