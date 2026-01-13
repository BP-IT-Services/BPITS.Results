# Custom Status Codes

Configure DefaultFailureValue and BadRequestValue to customize which enum values are used for common failure scenarios.

## DefaultFailureValue

Controls which status code is used when creating failures without explicitly specifying a status code:

```csharp
[GenerateServiceResult(DefaultFailureValue = nameof(InternalServerError))]
public enum MyAppStatus
{
    Ok = 0,
    BadRequest = 400,
    InternalServerError = 500
}

// This will use InternalServerError
var result = ServiceResult.Failure<User>("Something went wrong");
// result.StatusCode == MyAppStatus.InternalServerError
```

**Without DefaultFailureValue:**
```csharp
var result = ServiceResult.Failure<User>("Something went wrong");
// result.StatusCode will be the default enum value (typically 0)
```

## BadRequestValue

Controls which status code is used for validation failures:

```csharp
[GenerateServiceResult(BadRequestValue = nameof(ValidationError))]
public enum MyAppStatus
{
    Ok = 0,
    ValidationError = 400,
    InternalServerError = 500
}

// These validation methods will use ValidationError
var result1 = ServiceResult.ValidationFailure<User>("Invalid email");
var result2 = ServiceResult.ValidationFailure<User>("Email", "Email is required");
// Both have StatusCode == MyAppStatus.ValidationError
```

## Complete Example

```csharp
[GenerateApiResult(
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(ValidationFailed)
)]
[GenerateServiceResult(
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(ValidationFailed)
)]
public enum ApplicationStatusCode
{
    Ok = 0,
    ValidationFailed = 400,
    Unauthorized = 401,
    NotFound = 404,
    InternalServerError = 500
}
```

## Why Configure Custom Status Codes?

Proper configuration ensures consuming applications receive consistent status codes for programmatic error handling:

```csharp
// With proper configuration:
[GenerateApiResult(
    DefaultFailureValue = nameof(InternalServerError),
    BadRequestValue = nameof(ValidationError)
)]
public enum MyAppStatus
{
    Ok = 0,
    ValidationError = 400,
    InternalServerError = 500
}

// Consuming applications receive:
// - ValidationError for validation failures → Can implement field validation logic, show inline errors
// - InternalServerError for unexpected errors → Can implement retry logic with exponential backoff
// This enables consistent, predictable error handling in consuming applications
```

**Benefits for consuming applications:**
- **Predictable status codes**: Clients know what status codes to expect for each error type
- **Programmatic handling**: Same error types always use the same status code, enabling reliable switch statements
- **Appropriate logic**: Clients can implement different handling logic per error type (retry vs redirect vs show message)
- **Better UX**: Proper error handling enables appropriate user experiences

## Fallback Behavior

1. **DefaultFailureValue**: If not specified, uses the enum's default value (typically first member or value 0)
2. **BadRequestValue**: If not specified, looks for "BadRequest" enum member, falls back to default value

## See Also

- [Error Handling](../guides/error-handling.md) - Status code selection
- [Validation Patterns](../guides/validation-patterns.md) - Using validation failures
- [HTTP Status Mapping](http-status-mapping.md) - How clients receive status codes
