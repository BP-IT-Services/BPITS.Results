namespace BPITS.Results.Abstractions;

/// <summary>
/// Marks an enum to be used as the ResultStatusCode for generated ServiceResult classes.
/// The enum must contain an 'Ok' value.
/// ServiceResult is intended for internal use and includes detailed exception information.
/// </summary>
[AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
public sealed class GenerateServiceResultAttribute : Attribute
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
