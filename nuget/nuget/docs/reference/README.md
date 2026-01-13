# API Reference

Complete API documentation and best practices for BPITS.Results.

## Contents

- **[Best Practices](best-practices.md)** - Comprehensive best practices guide
- **[ServiceResult API](service-result-api.md)** - Complete ServiceResult documentation
- **[ApiResult API](api-result-api.md)** - Complete ApiResult documentation
- **[Attributes Reference](attributes.md)** - Source generator attributes

## Quick Reference

### Common Operations

- Creating success: `ServiceResult.Success(value)`
- Creating failure: `ServiceResult.Failure<T>("error", status)`
- Safe extraction: `if (result.TryGet(out var value))`
- Type transformation: `result.MapValue(v => v?.ToDto())`
- Error propagation: `result.PassThroughFail<T>()`

### ServiceResult vs ApiResult

- **ServiceResult**: Use in services, includes exception details
- **ApiResult**: Use in controllers, excludes exception details
- **Conversion**: `ApiResult.FromServiceResult(serviceResult)`

## See Also

- [Getting Started](../getting-started/) - New to BPITS.Results
- [User Guides](../guides/) - Common patterns
- [Advanced Topics](../advanced/) - In-depth techniques
