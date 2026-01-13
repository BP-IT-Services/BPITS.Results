# Result Chaining

Handle complex multi-step operations with proper error handling and propagation.

## Sequential Operations

Use TryGet and PassThroughFail for sequential operations:

```csharp
public async Task<ServiceResult<OrderDto>> CreateOrderAsync(CreateOrderRequest request)
{
    // Step 1: Validate and get user
    var userResult = await _userService.GetUserAsync(request.UserId);
    if (!userResult.TryGet(out var user))
        return userResult.PassThroughFail<OrderDto>();

    // Step 2: Validate and get products
    var productsResult = await _productService.GetProductsAsync(request.ProductIds);
    if (!productsResult.TryGet(out var products))
        return productsResult.PassThroughFail<OrderDto>();

    // Step 3: Validate inventory
    var inventoryCheck = await _inventoryService.CheckAvailabilityAsync(products);
    if (!inventoryCheck.IsSuccess)
        return inventoryCheck.PassThroughFail<OrderDto>();

    // All validations passed - create order
    var order = new Order(user, products);
    var createResult = await _orderRepository.CreateAsync(order);

    return createResult.MapValue(o => o?.ToDto());
}
```

## Early Returns Pattern

Exit early on first failure:

```csharp
public async Task<ServiceResult<Report>> GenerateReportAsync(Guid userId)
{
    // Get user
    var userResult = await GetUserAsync(userId);
    if (!userResult.TryGet(out var user))
        return userResult.PassThroughFail<Report>();

    // Get permissions
    if (!user.HasPermission("GenerateReports"))
        return ServiceResult.Failure<Report>("Unauthorized", MyStatus.Forbidden);

    // Get data
    var dataResult = await GetReportDataAsync(user);
    if (!dataResult.TryGet(out var data))
        return dataResult.PassThroughFail<Report>();

    // Generate report
    var report = GenerateReport(data);
    return report;
}
```

## Transaction Patterns

Combine with transactions for atomic operations:

```csharp
public async Task<ServiceResult<Order>> CreateOrderWithPaymentAsync(CreateOrderRequest request)
{
    using var transaction = await _dbContext.Database.BeginTransactionAsync();

    try
    {
        // Create order
        var orderResult = await CreateOrderInternalAsync(request);
        if (!orderResult.TryGet(out var order))
        {
            await transaction.RollbackAsync();
            return orderResult;
        }

        // Process payment
        var paymentResult = await _paymentService.ProcessPaymentAsync(order);
        if (!paymentResult.IsSuccess)
        {
            await transaction.RollbackAsync();
            return paymentResult.PassThroughFail<Order>();
        }

        await transaction.CommitAsync();
        return order;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return ServiceResult.Failure<Order>(ex, "Transaction failed");
    }
}
```

## See Also

- [Error Propagation](error-propagation.md) - PassThroughFail patterns
- [Working with Results](../guides/working-with-results.md) - Basic chaining
