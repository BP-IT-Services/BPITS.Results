namespace BPITS.Results.Abstractions;

/// <summary>
/// Marks an enum to be used as the ResultStatusCode for generated ApiResult classes.
/// The enum must contain an 'Ok' value.
/// ApiResult is intended for user-facing API responses and does not include internal exception details.
/// </summary>
[AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
public sealed class GenerateApiResultAttribute : Attribute
{
    /// <summary>
    /// Optional: The default failure status code to use when not specified.
    /// If not set, the generator will use the default enum value.
    /// </summary>
    public string? DefaultFailureValue { get; set; }

    /// <summary>
    /// Optional: The value to use for BadRequest/Validation failures.
    /// If not set, the generator will look for a 'BadRequest' value in the enum.
    /// </summary>
    public string? BadRequestValue { get; set; }
}
