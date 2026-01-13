# BPITS.Results Documentation

Welcome to the comprehensive documentation for BPITS.Results - a robust .NET implementation of the Result pattern with source generation for type-safe error handling.

## Getting Started

New to BPITS.Results? Start here:

1. **[Installation](getting-started/installation.md)** - Add BPITS.Results to your project
2. **[Quick Start](getting-started/quick-start.md)** - Your first Result implementation in 3 steps
3. **[Core Concepts](getting-started/core-concepts.md)** - Understanding ServiceResult vs ApiResult

Not sure where to begin? Check out the [Quick Start Guide](getting-started/quick-start.md) for a hands-on introduction.

## User Guides

Learn common patterns for day-to-day usage:

- **[Working with Results](guides/working-with-results.md)** - Creating, extracting, and converting results
- **[Validation Patterns](guides/validation-patterns.md)** - Handling validation errors and field-level details
- **[Controller Patterns](guides/controller-patterns.md)** - Using results in ASP.NET Core controllers
- **[Error Handling](guides/error-handling.md)** - Managing errors, status codes, and error details
- **[ASP.NET Core Integration](guides/aspnetcore-integration.md)** - Complete setup and usage guide

## Advanced Topics

Deep dives for experienced developers:

- **[Type Transformations](advanced/type-transformations.md)** - Mastering MapValue and type conversions
- **[Error Propagation](advanced/error-propagation.md)** - PassThroughFail patterns and error context
- **[Custom Status Codes](advanced/custom-status-codes.md)** - Configuring DefaultFailureValue and BadRequestValue
- **[HTTP Status Mapping](advanced/http-status-mapping.md)** - Automatic HTTP status code mapping in ASP.NET Core
- **[Result Chaining](advanced/result-chaining.md)** - Complex multi-step operations and workflows

## Integration Guides

Integrate with popular .NET libraries:

- **[Entity Framework](integration/entity-framework.md)** - Database operations and repository patterns
- **[FluentValidation](integration/fluentvalidation.md)** - Validation library integration
- **[Dependency Injection](integration/dependency-injection.md)** - DI patterns and testing strategies

## API Reference

Complete API documentation:

- **[ServiceResult API](reference/service-result-api.md)** - Complete ServiceResult documentation
- **[ApiResult API](reference/api-result-api.md)** - Complete ApiResult documentation
- **[Attributes Reference](reference/attributes.md)** - Source generator attributes
- **[Best Practices](reference/best-practices.md)** - Comprehensive best practices guide

## Package Documentation

Looking for package-specific information?

- **[BPITS.Results](../BPITS.Results/README.md)** - Core package documentation
- **[BPITS.Results.AspNetCore](../BPITS.Results.AspNetCore/README.md)** - ASP.NET Core integration package
- **[BPITS.Results.Abstractions](../BPITS.Results.Abstractions/README.md)** - Core abstractions package
- **[BPITS.Results.AspNetCore.Abstractions](../BPITS.Results.AspNetCore.Abstractions/README.md)** - ASP.NET Core abstractions package

## Documentation Structure

This documentation is organized to support progressive learning:

- **Getting Started** - For developers new to the Result pattern or BPITS.Results
- **Guides** - For day-to-day usage patterns and common scenarios
- **Advanced Topics** - For experienced developers needing in-depth techniques
- **Integration** - For integrating with specific third-party libraries
- **Reference** - For API lookup and comprehensive best practices

## Quick Links by Topic

### Error Handling
- [Creating failure results](guides/working-with-results.md#creating-results)
- [Validation errors](guides/validation-patterns.md)
- [Custom status codes](advanced/custom-status-codes.md)
- [Error propagation](advanced/error-propagation.md)

### Type Conversions
- [Basic MapValue usage](guides/working-with-results.md#type-conversion-with-mapvalue)
- [Advanced transformations](advanced/type-transformations.md)
- [Converting ServiceResult to ApiResult](guides/working-with-results.md#converting-between-result-types)

### ASP.NET Core
- [Complete setup guide](guides/aspnetcore-integration.md)
- [HTTP status mapping](advanced/http-status-mapping.md)
- [Controller patterns](guides/controller-patterns.md)

### Integration
- [Entity Framework patterns](integration/entity-framework.md)
- [FluentValidation integration](integration/fluentvalidation.md)
- [Dependency injection](integration/dependency-injection.md)

## Contributing

Found an issue or want to improve the documentation? Contributions are welcome!

- [GitHub Repository](https://github.com/BP-IT-Services/BPITS.Results)
- [Report Issues](https://github.com/BP-IT-Services/BPITS.Results/issues)

## Need Help?

- Check the [Best Practices](reference/best-practices.md) guide
- Review the [API Reference](reference/)
- Search the [GitHub Issues](https://github.com/BP-IT-Services/BPITS.Results/issues)
- Open a [new issue](https://github.com/BP-IT-Services/BPITS.Results/issues/new)
