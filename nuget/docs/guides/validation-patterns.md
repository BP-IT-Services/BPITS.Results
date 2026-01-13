# Validation Patterns

Learn how to handle validation errors effectively with BPITS.Results, including single-field and multi-field validation scenarios.

## Table of Contents

- [Overview](#overview)
- [Basic Validation Failures](#basic-validation-failures)
- [Single Field Validation](#single-field-validation)
- [Multiple Field Validation](#multiple-field-validation)
- [Complex Validation Scenarios](#complex-validation-scenarios)
- [Working with Error Details](#working-with-error-details)
- [Integration with FluentValidation](#integration-with-fluentvalidation)
- [Best Practices](#best-practices)

## Overview

BPITS.Results provides special support for validation errors through `ValidationFailure` methods. These methods automatically use the configured `BadRequestValue` status code and structure error details for client consumption.

## Basic Validation Failures

For simple validation errors without field-level details:

```csharp
public ServiceResult<User> CreateUser(CreateUserRequest request)
{
    // Simple validation check
    if (string.IsNullOrEmpty(request.Email))
    {
        return ServiceResult.ValidationFailure<User>("Email address is required");
    }

    // Create user...
    return ServiceResult.Success(newUser);
}
```

This creates a failure with:
- `StatusCode` = configured `BadRequestValue` (or the enum's `BadRequest` value)
- `ErrorMessage` = "Email address is required"
- `ErrorDetails` = null

## Single Field Validation

For validation errors related to a specific field:

### Single Error Per Field

```csharp
public ServiceResult<User> CreateUser(CreateUserRequest request)
{
    if (string.IsNullOrEmpty(request.Email))
    {
        return ServiceResult.ValidationFailure<User>(
            fieldName: "Email",
            errorMessage: "Email address is required"
        );
    }

    if (!IsValidEmail(request.Email))
    {
        return ServiceResult.ValidationFailure<User>(
            fieldName: "Email",
            errorMessage: "Email address must be in a valid format"
        );
    }

    // Create user...
    return newUser;
}
```

This creates a failure with:
- `StatusCode` = `BadRequestValue`
- `ErrorMessage` = "Email address is required" (or the second error)
- `ErrorDetails` = `{ "Email": ["Email address is required"] }`

### Multiple Errors for One Field

```csharp
public ServiceResult<User> UpdatePassword(UpdatePasswordRequest request)
{
    var passwordErrors = new List<string>();

    if (request.NewPassword.Length < 8)
        passwordErrors.Add("Password must be at least 8 characters long");

    if (!ContainsUppercase(request.NewPassword))
        passwordErrors.Add("Password must contain at least one uppercase letter");

    if (!ContainsNumber(request.NewPassword))
        passwordErrors.Add("Password must contain at least one number");

    if (passwordErrors.Any())
    {
        return ServiceResult.ValidationFailure<User>(
            fieldName: "NewPassword",
            errorMessages: passwordErrors.ToArray()
        );
    }

    // Update password...
    return updatedUser;
}
```

This creates:
- `ErrorDetails["NewPassword"]` = `["Password must be at least 8 characters...", "Password must contain...", ...]`

## Multiple Field Validation

For validation across multiple fields:

### Dictionary-Based Validation

```csharp
public ServiceResult<User> CreateUser(CreateUserRequest request)
{
    var validationErrors = new Dictionary<string, string[]>();

    // Validate email
    if (string.IsNullOrEmpty(request.Email))
        validationErrors["Email"] = new[] { "Email is required" };
    else if (!IsValidEmail(request.Email))
        validationErrors["Email"] = new[] { "Email must be in a valid format" };

    // Validate name
    if (string.IsNullOrEmpty(request.Name))
        validationErrors["Name"] = new[] { "Name is required" };
    else if (request.Name.Length < 2)
        validationErrors["Name"] = new[] { "Name must be at least 2 characters long" };

    // Validate password
    var passwordErrors = new List<string>();
    if (string.IsNullOrEmpty(request.Password))
        passwordErrors.Add("Password is required");
    if (request.Password?.Length < 8)
        passwordErrors.Add("Password must be at least 8 characters");

    if (passwordErrors.Any())
        validationErrors["Password"] = passwordErrors.ToArray();

    // Return all validation errors at once
    if (validationErrors.Any())
    {
        return ServiceResult.ValidationFailure<User>(
            "Validation failed",
            validationErrors
        );
    }

    // All validations passed, create user
    var newUser = new User
    {
        Email = request.Email,
        Name = request.Name
    };

    return newUser;
}
```

The resulting JSON response (when converted to ApiResult):

```json
{
  "isSuccess": false,
  "isFailure": true,
  "statusCode": 400,
  "errorMessage": "Validation failed",
  "errorDetails": {
    "Email": ["Email must be in a valid format"],
    "Name": ["Name is required"],
    "Password": [
      "Password is required",
      "Password must be at least 8 characters"
    ]
  },
  "value": null
}
```

### Builder Pattern for Validation

For complex validation, consider a builder pattern:

```csharp
public class ValidationErrorBuilder
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public ValidationErrorBuilder AddError(string fieldName, string errorMessage)
    {
        if (!_errors.ContainsKey(fieldName))
            _errors[fieldName] = new List<string>();

        _errors[fieldName].Add(errorMessage);
        return this;
    }

    public bool HasErrors => _errors.Any();

    public Dictionary<string, string[]> Build()
    {
        return _errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray()
        );
    }
}

// Usage
public ServiceResult<User> CreateUser(CreateUserRequest request)
{
    var validation = new ValidationErrorBuilder();

    // Email validation
    if (string.IsNullOrEmpty(request.Email))
        validation.AddError("Email", "Email is required");
    else if (!IsValidEmail(request.Email))
        validation.AddError("Email", "Email must be valid");

    // Name validation
    if (string.IsNullOrEmpty(request.Name))
        validation.AddError("Name", "Name is required");

    // Age validation
    if (request.Age < 18)
        validation.AddError("Age", "Must be at least 18 years old");
    else if (request.Age > 120)
        validation.AddError("Age", "Age seems invalid");

    if (validation.HasErrors)
    {
        return ServiceResult.ValidationFailure<User>(
            "Validation failed",
            validation.Build()
        );
    }

    // Create user...
    return newUser;
}
```

## Complex Validation Scenarios

### Conditional Validation

Validate fields conditionally based on other fields:

```csharp
public ServiceResult<Order> CreateOrder(CreateOrderRequest request)
{
    var errors = new Dictionary<string, string[]>();

    // Basic validation
    if (request.Items == null || !request.Items.Any())
        errors["Items"] = new[] { "At least one item is required" };

    // Conditional validation based on shipping method
    if (request.ShippingMethod == ShippingMethod.HomeDelivery)
    {
        if (string.IsNullOrEmpty(request.ShippingAddress))
            errors["ShippingAddress"] = new[] { "Shipping address is required for home delivery" };
    }

    // Conditional validation based on payment method
    if (request.PaymentMethod == PaymentMethod.CreditCard)
    {
        if (string.IsNullOrEmpty(request.CardNumber))
            errors["CardNumber"] = new[] { "Card number is required" };

        if (string.IsNullOrEmpty(request.CardExpiry))
            errors["CardExpiry"] = new[] { "Card expiry is required" };
    }

    if (errors.Any())
        return ServiceResult.ValidationFailure<Order>("Validation failed", errors);

    // Create order...
    return newOrder;
}
```

### Cross-Field Validation

Validate relationships between fields:

```csharp
public ServiceResult<Appointment> ScheduleAppointment(ScheduleAppointmentRequest request)
{
    var errors = new Dictionary<string, string[]>();

    // Individual field validation
    if (request.StartTime == default)
        errors["StartTime"] = new[] { "Start time is required" };

    if (request.EndTime == default)
        errors["EndTime"] = new[] { "End time is required" };

    // Cross-field validation
    if (request.EndTime <= request.StartTime)
    {
        errors["EndTime"] = new[] { "End time must be after start time" };
    }

    if ((request.EndTime - request.StartTime).TotalHours > 8)
    {
        errors["EndTime"] = new[] { "Appointment cannot exceed 8 hours" };
    }

    if (request.StartTime < DateTime.UtcNow)
    {
        errors["StartTime"] = new[] { "Cannot schedule appointments in the past" };
    }

    if (errors.Any())
        return ServiceResult.ValidationFailure<Appointment>("Validation failed", errors);

    // Schedule appointment...
    return newAppointment;
}
```

## Working with Error Details

### Accessing Error Details

```csharp
var result = await _userService.CreateUserAsync(request);

if (result.IsFailure && result.ErrorDetails != null)
{
    foreach (var (fieldName, errors) in result.ErrorDetails)
    {
        Console.WriteLine($"{fieldName}:");
        foreach (var error in errors)
        {
            Console.WriteLine($"  - {error}");
        }
    }
}

// Output:
// Email:
//   - Email is required
// Password:
//   - Password must be at least 8 characters
//   - Password must contain a number
```

### Client-Side Error Display

In a web application, error details can be easily bound to form fields:

```csharp
[HttpPost]
public async Task<ApiResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
{
    var result = await _userService.CreateUserAsync(request);
    return ApiResult.FromServiceResult(result.MapValue(u => u?.ToDto()));
}

// Client receives JSON:
{
  "errorDetails": {
    "Email": ["Email is required"],
    "Password": ["Password must be at least 8 characters"]
  }
}

// Easy to bind to form fields in JavaScript:
for (const [field, errors] of Object.entries(response.errorDetails)) {
  displayFieldErrors(field, errors);
}
```

## Integration with FluentValidation

BPITS.Results works seamlessly with FluentValidation:

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
            .MinimumLength(8).WithMessage("Password must be at least 8 characters");
    }
}

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

        // Create user...
        return newUser;
    }
}
```

See the [FluentValidation Integration](../integration/fluentvalidation.md) guide for more details.

## Best Practices

### 1. Validate Early, Fail Fast

```csharp
// Good: Validate at the start of the method
public ServiceResult<User> CreateUser(CreateUserRequest request)
{
    // Collect all validation errors first
    var errors = ValidateRequest(request);
    if (errors.Any())
        return ServiceResult.ValidationFailure<User>("Validation failed", errors);

    // Proceed with business logic only if validation passes
    return CreateUserInternal(request);
}

// Avoid: Scattered validation checks
public ServiceResult<User> CreateUser(CreateUserRequest request)
{
    var user = new User { Email = request.Email };

    if (string.IsNullOrEmpty(request.Email)) // Too late!
        return ServiceResult.ValidationFailure<User>("Email required");

    // More logic...
}
```

### 2. Return All Validation Errors at Once

```csharp
// Good: Collect and return all errors
var errors = new Dictionary<string, string[]>();
// ... add all validation errors
if (errors.Any())
    return ServiceResult.ValidationFailure<User>("Validation failed", errors);

// Avoid: Returning on first error (requires multiple round-trips)
if (string.IsNullOrEmpty(request.Email))
    return ServiceResult.ValidationFailure<User>("Email", "Email is required");

if (string.IsNullOrEmpty(request.Name)) // Client won't see this until email is fixed
    return ServiceResult.ValidationFailure<User>("Name", "Name is required");
```

### 3. Use Consistent Field Names

```csharp
// Good: Match the property names in your request DTOs
return ServiceResult.ValidationFailure<User>(
    "Email", // Matches CreateUserRequest.Email
    "Email is required"
);

// Avoid: Inconsistent casing or naming
return ServiceResult.ValidationFailure<User>(
    "email", // lowercase doesn't match property
    "Email is required"
);
```

This allows clients to easily bind errors to form fields.

### 4. Provide Actionable Error Messages

```csharp
// Good: Specific, actionable messages
"Email address must be in a valid format (e.g., user@example.com)"
"Password must contain at least one uppercase letter, one number, and be at least 8 characters"

// Avoid: Vague or unhelpful messages
"Invalid input"
"Bad request"
"Validation error"
```

### 5. Separate Validation Logic

```csharp
// Good: Separate validation into its own method
private Dictionary<string, string[]> ValidateCreateUserRequest(CreateUserRequest request)
{
    var errors = new Dictionary<string, string[]>();

    // All validation logic here
    if (string.IsNullOrEmpty(request.Email))
        errors["Email"] = new[] { "Email is required" };

    // ... more validation

    return errors;
}

public ServiceResult<User> CreateUser(CreateUserRequest request)
{
    var errors = ValidateCreateUserRequest(request);
    if (errors.Any())
        return ServiceResult.ValidationFailure<User>("Validation failed", errors);

    // Clean business logic
    return CreateUserInternal(request);
}
```

This keeps methods focused and validation logic reusable.

## See Also

- [Working with Results](working-with-results.md) - Core result operations
- [Error Handling](error-handling.md) - Managing errors and status codes
- [FluentValidation Integration](../integration/fluentvalidation.md) - Using FluentValidation with BPITS.Results
- [Controller Patterns](controller-patterns.md) - Returning validation errors from controllers
- [Best Practices](../reference/best-practices.md) - Comprehensive best practices

## Next Steps

- Learn about [Error Handling](error-handling.md) for managing different error types
- Explore [Controller Patterns](controller-patterns.md) for returning validation errors from APIs
- Check out the [FluentValidation Integration](../integration/fluentvalidation.md) guide
