# Error Propagation

Learn how to propagate errors through multiple layers using PassThroughFail.

## PassThroughFail Method

When chaining operations, use `PassThroughFail<T>()` to propagate failures while changing the result type:

```csharp
public async Task<ServiceResult<ProcessedData>> ProcessUserDataAsync(Guid userId)
{
    // Get user (returns ServiceResult<User>)
    var userResult = await GetUserAsync(userId);
    if (!userResult.TryGet(out var user))
    {
        // Propagate the failure but change return type to ProcessedData
        return userResult.PassThroughFail<ProcessedData>();
    }

    // Continue processing with user
    return ProcessData(user);
}
```

**What PassThroughFail does:**
- Preserves the error message
- Preserves the status code
- Preserves error details
- Preserves the exception (for ServiceResult)
- Changes the generic type parameter

## Multi-Step Error Propagation

```csharp
public async Task<ServiceResult<OrderDto>> CreateOrderAsync(CreateOrderRequest request)
{
    // Step 1: Get user
    var userResult = await _userService.GetUserAsync(request.UserId);
    if (!userResult.TryGet(out var user))
        return userResult.PassThroughFail<OrderDto>();

    // Step 2: Get products
    var productsResult = await _productService.GetProductsAsync(request.ProductIds);
    if (!productsResult.TryGet(out var products))
        return productsResult.PassThroughFail<OrderDto>();

    // Step 3: Check inventory
    var inventoryResult = await _inventoryService.CheckAsync(products);
    if (!inventoryResult.IsSuccess)
        return inventoryResult.PassThroughFail<OrderDto>();

    // All steps succeeded - create order
    var order = new Order(user, products);
    var createResult = await _orderRepository.CreateAsync(order);

    return createResult.MapValue(o => o?.ToDto());
}
```

## See Also

- [Result Chaining](result-chaining.md) - Complex multi-step operations
- [Working with Results](../guides/working-with-results.md) - Basic chaining patterns
