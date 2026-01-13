# HTTP Status Mapping

Deep dive into how BPITS.Results.AspNetCore maps custom status codes to HTTP status codes.

## How It Works

When you apply `[EnableApiResultMapping]` to your enum, the source generator creates:

1. A partial `ApiResult<TEnum>` class implementing `IActionResult`
2. An `IActionResultMapper<TEnum>` implementation
3. A DI extension method to register the mapper

## The HttpStatusCode Attribute

Map enum values to HTTP status codes:

```csharp
[EnableApiResultMapping]
public enum MyApiStatus
{
    [HttpStatusCode(HttpStatusCode.OK)]
    Success = 0,          // Returns HTTP 200

    [HttpStatusCode(HttpStatusCode.NotFound)]
    UserNotFound = 1,     // Returns HTTP 404

    [HttpStatusCode(HttpStatusCode.NotFound)]
    ProductNotFound = 2,  // Also returns HTTP 404
}
```

**Multiple enum values can map to the same HTTP status code**, allowing domain-specific codes that share HTTP semantics.

## Generated Mapper

The source generator creates a mapper like this:

```csharp
public class MyApiStatusActionResultMapper : IActionResultMapper<MyApiStatus>
{
    public int MapToHttpStatusCode(MyApiStatus statusCode)
    {
        return statusCode switch
        {
            MyApiStatus.Success => 200,
            MyApiStatus.UserNotFound => 404,
            MyApiStatus.ProductNotFound => 404,
            _ => 500 // Fallback for unmapped codes
        };
    }
}
```

## Architecture

```
ApiResult<TEnum>
    ↓
ExecuteResultAsync (from IActionResult)
    ↓
Gets IActionResultMapper<TEnum> from DI
    ↓
MapToHttpStatusCode(result.StatusCode)
    ↓
Sets HTTP response status code
    ↓
Serializes result to JSON
```

## Why This Matters for Consuming Applications

Custom status codes enable consuming applications (frontends, mobile apps) to handle errors programmatically:

- **Programmatic error handling**: Frontend receives the custom status code in the JSON response and can switch on it to implement appropriate logic
- **No string parsing**: Handle errors based on enum values, not parsing error message strings
- **Consistent semantics**: Different domain errors (`UserNotFound` vs `SubscriptionExpired`) trigger appropriate handling logic
- **Flexibility**: Change HTTP status code mapping without affecting client error handling logic
- **Better UX**: Proper error handling enables appropriate user experiences (retry, redirect, show message, etc.)

**Example**:
```javascript
// Frontend JavaScript
const response = await fetch('/api/users/123');
const result = await response.json();

// Client switches on the custom status code for programmatic error handling
switch (result.statusCode) {
    case 'UserNotFound':
        // Could retry, redirect to search, or show message
        redirectToUserSearch();
        break;
    case 'AccountLocked':
        // Redirect to account recovery flow
        redirectToAccountRecovery();
        break;
    case 'SubscriptionExpired':
        // Disable premium features and redirect to upgrade
        disablePremiumFeatures();
        redirectToUpgrade();
        break;
    case 'InternalServerError':
        // Retry with exponential backoff
        retryWithBackoff();
        break;
}
```

## Benefits

- **Programmatic error handling**: Consuming applications use custom status codes to implement appropriate logic for each error type
- **No string parsing**: Error handling based on enum values, not parsing error messages
- **Separation of concerns**: Business logic uses domain codes, HTTP layer uses HTTP codes
- **Flexibility**: Change HTTP mapping without changing business logic or client code
- **Better UX**: Proper error handling enables appropriate user experiences
- **Zero overhead**: All code generated at compile time

## See Also

- [ASP.NET Core Integration](../guides/aspnetcore-integration.md) - Complete setup guide
- [Custom Status Codes](custom-status-codes.md) - Configuring status codes
