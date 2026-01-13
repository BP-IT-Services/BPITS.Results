# User Guides

Practical guides for common scenarios and day-to-day usage patterns with BPITS.Results.

## Overview

These guides cover the most common patterns you'll use when working with BPITS.Results. Each guide focuses on a specific aspect of result handling, with real-world examples and best practices.

## Essential Guides

### [Working with Results](working-with-results.md)

The foundational guide for result manipulation. Learn how to create, extract, and transform results effectively.

**Topics covered:**
- Creating success and failure results
- Safe value extraction with TryGet
- Type transformations with MapValue
- Converting between ServiceResult and ApiResult
- Implicit conversions

**When to read**: Start here after completing the Quick Start guide.

### [Error Handling](error-handling.md)

Master error management with status codes and error details.

**Topics covered:**
- Working with error details
- Status code selection strategies
- Error message best practices
- Checking for specific error conditions
- Logging errors effectively

**When to read**: When you need to handle different types of errors.

### [Validation Patterns](validation-patterns.md)

Handle validation errors with field-level details and multiple validation scenarios.

**Topics covered:**
- Single field validation failures
- Multiple field validation errors
- Validation error details structure
- Complex validation scenarios
- Best practices for validation

**When to read**: When implementing input validation in your application.

## Controller & API Guides

### [Controller Patterns](controller-patterns.md)

Best practices for using results in ASP.NET Core controllers.

**Topics covered:**
- Basic controller actions with results
- Handling paged results
- Exception handling in controllers
- Testing controller actions
- Common controller patterns

**When to read**: When building ASP.NET Core APIs.

### [ASP.NET Core Integration](aspnetcore-integration.md)

Complete setup and usage guide for ASP.NET Core integration with automatic HTTP status code mapping.

**Topics covered:**
- Complete installation and setup
- Registering the action result mapper
- Using ApiResult as IActionResult
- HTTP status code mapping
- Response serialization
- Complete working examples

**When to read**: When you want automatic HTTP status code mapping for your APIs.

## Guide Format

Each guide follows a consistent structure:
- **Overview** - What the guide covers
- **Basic Usage** - Simple examples to get started
- **Common Scenarios** - Real-world use cases
- **Best Practices** - Recommendations and tips
- **See Also** - Related guides and topics

## Recommended Reading Order

1. **[Working with Results](working-with-results.md)** - Start here for fundamental patterns
2. **[Validation Patterns](validation-patterns.md)** - Learn validation handling
3. **[Error Handling](error-handling.md)** - Master error management
4. **[Controller Patterns](controller-patterns.md)** - Apply results in controllers
5. **[ASP.NET Core Integration](aspnetcore-integration.md)** - Optional: Set up HTTP status mapping

## Quick Links by Scenario

### I need to...

**Create Results:**
- [Create a success result](working-with-results.md#creating-success-results)
- [Create a failure result](working-with-results.md#creating-failure-results)
- [Create a validation failure](validation-patterns.md#basic-validation-failures)

**Extract Values:**
- [Safely extract values with TryGet](working-with-results.md#safe-value-extraction-with-tryget)
- [Get value or throw](working-with-results.md#get-method)
- [Access value directly](working-with-results.md#direct-property-access)

**Transform Results:**
- [Convert types with MapValue](working-with-results.md#type-conversion-with-mapvalue)
- [Convert ServiceResult to ApiResult](working-with-results.md#serviceresult-to-apiresult)
- [Handle null values in transformations](working-with-results.md#handling-null-values)

**Handle Errors:**
- [Check for specific error types](error-handling.md#checking-for-specific-errors)
- [Work with error details](error-handling.md#working-with-error-details)
- [Log errors effectively](error-handling.md#logging-errors)

**Validate Input:**
- [Single field validation](validation-patterns.md#single-field-validation)
- [Multiple field validation](validation-patterns.md#multiple-field-validation)
- [Complex validation scenarios](validation-patterns.md#complex-validation)

**Use in Controllers:**
- [Basic controller action](controller-patterns.md#basic-controller-actions)
- [Handle paged results](controller-patterns.md#paged-results)
- [Exception handling](controller-patterns.md#exception-handling)

## Next Steps

After completing the guides:

- **[Advanced Topics](../advanced/)** - Deep dives into advanced techniques
- **[Integration Guides](../integration/)** - Integrate with EF Core, FluentValidation, etc.
- **[Best Practices](../reference/best-practices.md)** - Comprehensive best practices
- **[API Reference](../reference/)** - Complete API documentation

## Need Help?

- Check the [getting started guides](../getting-started/)
- Review the [best practices](../reference/best-practices.md)
- Search [GitHub Issues](https://github.com/BP-IT-Services/BPITS.Results/issues)
