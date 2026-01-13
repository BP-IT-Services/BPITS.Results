# Advanced Topics

In-depth exploration of advanced techniques and patterns for power users of BPITS.Results.

## Overview

These guides cover advanced scenarios that go beyond day-to-day usage. Topics include advanced type transformations, error propagation patterns, custom status code configuration, and complex operation chaining.

## Available Topics

### [Error Propagation](error-propagation.md)

Learn how to propagate errors through multiple layers and operations using PassThroughFail.

**Topics covered:**
- PassThroughFail method explained
- Error propagation patterns
- Maintaining error context across operations
- Multi-layer error propagation

**When to read**: When chaining multiple operations that may fail

### [Custom Status Codes](custom-status-codes.md)

Configure DefaultFailureValue and BadRequestValue for your status code enums.

**Topics covered:**
- DefaultFailureValue configuration
- BadRequestValue configuration
- Impact on generated methods
- Fallback behavior
- Complete configuration examples

**When to read**: When you need fine-grained control over default status codes

### [HTTP Status Mapping](http-status-mapping.md)

Deep dive into how BPITS.Results.AspNetCore maps custom status codes to HTTP status codes.

**Topics covered:**
- How HttpStatusCode attribute works
- Understanding the generated mapper
- Architecture overview
- Custom mapper implementation

**When to read**: When using ASP.NET Core integration and need to understand the mapping mechanism

### [Type Transformations](type-transformations.md)

Master advanced MapValue techniques for complex type transformations.

**Topics covered:**
- MapValue deep dive
- MapValueWhenNotNull patterns
- Conditional mapping strategies
- Chaining multiple transformations
- Performance considerations

**When to read**: When dealing with complex type transformation scenarios

### [Result Chaining](result-chaining.md)

Handle complex multi-step operations with proper error handling and propagation.

**Topics covered:**
- Sequential vs parallel operations
- Early returns with TryGet
- Complex workflows
- Transaction patterns
- Error handling in chains

**When to read**: When implementing complex business operations with multiple steps

## Prerequisites

Before diving into advanced topics, ensure you're comfortable with:
- [Core Concepts](../getting-started/core-concepts.md)
- [Working with Results](../guides/working-with-results.md)
- [Validation Patterns](../guides/validation-patterns.md)

## See Also

- [User Guides](../guides/) - Day-to-day usage patterns
- [Integration Guides](../integration/) - Third-party library integrations
- [API Reference](../reference/) - Complete API documentation
