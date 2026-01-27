# FluentValidation Integration

Integrate BPITS.Results with FluentValidation for comprehensive input validation.

## Basic Integration

```csharp
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter")
            .Matches("[0-9]").WithMessage("Password must contain a number");
    }
}
```

## Converting ValidationResult to ServiceResult

```csharp
public class UserService
{
    private readonly IValidator<CreateUserRequest> _validator;

    public async Task<ServiceResult<User>> CreateUserAsync(CreateUserRequest request)
    {
        // Validate using FluentValidation
        var validationResult = await _validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            // Convert FluentValidation errors to Result error details
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return ServiceResult.ValidationFailure<User>("Validation failed", errors);
        }

        // Proceed with creation
        var user = new User
        {
            Email = request.Email,
            Name = request.Name
        };

        return await _repository.CreateAsync(user);
    }
}
```

## Reusable Extension Method

```csharp
public static class FluentValidationExtensions
{
    public static ServiceResult<T> ToServiceResult<T>(this ValidationResult validationResult)
    {
        if (validationResult.IsValid)
            throw new InvalidOperationException("Cannot convert successful validation to failure result");

        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return ServiceResult.ValidationFailure<T>("Validation failed", errors);
    }
}

// Usage
public async Task<ServiceResult<User>> CreateUserAsync(CreateUserRequest request)
{
    var validationResult = await _validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return validationResult.ToServiceResult<User>();

    // Create user...
}
```

## Related

- [Validation Patterns](../guides/validation-patterns.md)
- [Working with Results](../guides/working-with-results.md)
