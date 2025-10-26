namespace BPITS.Results.AspNetCore.Abstractions;

/// <summary>
/// Enables ASP.NET Core HTTP status code mapping for ApiResult types.
/// When applied to an enum (along with ResultStatusCode), generates:
/// - IActionResultMapper implementation for HTTP status code mapping
/// - IActionResult implementation for ApiResult types
/// - ServiceCollection extension methods for dependency injection
/// </summary>
[AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
public sealed class EnableApiResultMappingAttribute : Attribute
{
}