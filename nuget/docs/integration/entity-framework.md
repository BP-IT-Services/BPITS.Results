# Entity Framework Integration

Integrate BPITS.Results with Entity Framework Core for robust database operations.

## Basic Repository Pattern

```csharp
public class UserRepository
{
    private readonly AppDbContext _context;

    public async Task<ServiceResult<User>> GetByIdAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return ServiceResult.Failure<User>("User not found", MyStatus.NotFound);

            return user;
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure<User>(ex, "Failed to retrieve user");
        }
    }

    public async Task<ServiceResult<User>> CreateAsync(User user)
    {
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            return ServiceResult.ValidationFailure<User>("Email address is already in use");
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure<User>(ex, "Failed to create user");
        }
    }
}
```

## Handling Constraint Violations

```csharp
public async Task<ServiceResult<Order>> CreateOrderAsync(Order order)
{
    try
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }
    catch (DbUpdateException ex)
    {
        // Handle specific constraint violations
        if (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
            return ServiceResult.Failure<Order>("Order number already exists", MyStatus.Conflict);

        if (ex.InnerException?.Message.Contains("FOREIGN KEY constraint failed") == true)
            return ServiceResult.Failure<Order>("Invalid reference", MyStatus.BadRequest);

        // General database error
        return ServiceResult.Failure<Order>(ex, "Database error occurred");
    }
}
```

## Transaction Patterns

```csharp
public async Task<ServiceResult<Order>> CreateOrderWithItemsAsync(Order order, List<OrderItem> items)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // Add order
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Add items
        foreach (var item in items)
        {
            item.OrderId = order.Id;
            _context.OrderItems.Add(item);
        }
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
        return order;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return ServiceResult.Failure<Order>(ex, "Failed to create order");
    }
}
```

## Related

- [Working with Results](../guides/working-with-results.md)
- [Error Handling](../guides/error-handling.md)
