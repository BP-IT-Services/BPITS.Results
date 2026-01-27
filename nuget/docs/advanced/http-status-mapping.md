# HTTP Status Mapping

How BPITS.Results.AspNetCore maps custom status codes to HTTP status codes.

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

## Custom Status Codes and Consuming Applications

Custom status codes let consuming applications handle errors programmatically — switching on domain-specific codes rather than parsing error strings. See [Core Concepts — Why Use Custom Status Codes?](../getting-started/core-concepts.md#why-use-custom-status-codes) for the full rationale.

The key architectural benefit: your business logic uses domain codes (`UserNotFound`, `SubscriptionExpired`), the HTTP layer maps them to standard HTTP status codes, and clients can use either depending on their needs.

```javascript
// Frontend JavaScript
const response = await fetch('/api/users/123');
const result = await response.json();

switch (result.statusCode) {
    case 'UserNotFound':
        redirectToUserSearch();
        break;
    case 'AccountLocked':
        redirectToAccountRecovery();
        break;
    case 'SubscriptionExpired':
        disablePremiumFeatures();
        redirectToUpgrade();
        break;
    case 'InternalServerError':
        retryWithBackoff();
        break;
}
```

## Related

- [ASP.NET Core Integration](../guides/aspnetcore-integration.md) - Complete setup guide
- [Custom Status Codes](custom-status-codes.md) - Configuring status codes
