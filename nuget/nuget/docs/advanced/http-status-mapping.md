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

## Benefits

- **Separation of concerns**: Business logic uses domain codes, HTTP layer uses HTTP codes
- **Flexibility**: Change HTTP mapping without changing business logic
- **Type safety**: Compiler ensures status codes are valid
- **Zero overhead**: All code generated at compile time

## See Also

- [ASP.NET Core Integration](../guides/aspnetcore-integration.md) - Complete setup guide
- [Custom Status Codes](custom-status-codes.md) - Configuring status codes
