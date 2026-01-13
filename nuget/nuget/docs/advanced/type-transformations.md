# Type Transformations

Master advanced MapValue techniques for complex type transformations.

## MapValue Deep Dive

`MapValue` transforms the result's value type while preserving status and error information:

```csharp
ServiceResult<User> userResult = await GetUserAsync(id);
ServiceResult<UserDto> dtoResult = userResult.MapValue(user => user?.ToDto());

// If userResult was successful: dtoResult is successful with DTO
// If userResult failed: dtoResult fails with same error
```

## Null Handling Strategies

### MapValueWhenNotNull

Only transforms if value is not null:

```csharp
var result = serviceResult.MapValueWhenNotNull(user => user.ToDto());
// Equivalent to: serviceResult.MapValue(user => user?.ToDto())
```

### Custom Null Handling

Provide different functions for null and non-null cases:

```csharp
var result = serviceResult.MapValue(
    whenValueNotNullFunc: user => new UserDto
    {
        Id = user.Id,
        Name = user.Name,
        IsActive = true
    },
    whenValueNullFunc: _ => new UserDto
    {
        Name = "Unknown User",
        IsActive = false
    }
);
```

## Chaining Transformations

Chain multiple MapValue calls for multi-step transformations:

```csharp
var result = serviceResult
    .MapValue(user => user?.EnrichWithMetadata())     // Step 1: Enrich
    .MapValue(enriched => enriched?.ToDto())          // Step 2: Convert to DTO
    .MapValue(dto => dto?.ApplyBusinessRules())       // Step 3: Apply rules
    .MapValue(final => final?.ToViewModel());         // Step 4: To view model

// Errors from any step flow through entire chain
```

## Complex Transformations

### Conditional Transformations

```csharp
var result = serviceResult.MapValue(user =>
{
    if (user == null) return null;

    return user.IsActive
        ? user.ToActiveUserDto()
        : user.ToInactiveUserDto();
});
```

### Transforming Collections

```csharp
ServiceResult<List<User>> usersResult = await GetUsersAsync();
ServiceResult<List<UserDto>> dtosResult = usersResult.MapValue(users =>
    users?.Select(u => u.ToDto()).ToList()
);
```

## Performance Considerations

- **MapValue is lightweight** - no reflection, just function calls
- **Chaining is efficient** - no intermediate allocations
- **Avoid heavy computations in MapValue** - keep transformations simple

## See Also

- [Working with Results](../guides/working-with-results.md) - Basic MapValue usage
- [Error Propagation](error-propagation.md) - Combining with PassThroughFail
