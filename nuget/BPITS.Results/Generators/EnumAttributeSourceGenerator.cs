using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BPITS.Results.Generators;

[Generator]
public class EnumAttributeSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Define the attribute that users will apply to their enum
        context.RegisterPostInitializationOutput(Execute);
    }

    private static void Execute(IncrementalGeneratorPostInitializationContext context)
    {
        var attributeSource = GenerateResultStatusCodeAttribute(Constants.AttributeNamespace);
        context.AddSource("ResultStatusCodeAttribute.g.cs", SourceText.From(attributeSource, Encoding.UTF8));
    }

    private static string GenerateResultStatusCodeAttribute(string attributeNamespace)
    {
        return $@"#nullable enable
using System;

namespace {attributeNamespace}
{{
    /// <summary>
    /// Marks an enum to be used as the ResultStatusCode for generated ServiceResult and ApiResult classes.
    /// The enum must contain an 'Ok' value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    public sealed class ResultStatusCodeAttribute : Attribute
    {{
        /// <summary>
        /// Optional: The default failure status code to use when not specified.
        /// If not set, the generator will use the default enum value.
        /// </summary>
        public string? DefaultFailureValue {{ get; set; }}
        
        /// <summary>
        /// Optional: The value to use for BadRequest/Validation failures.
        /// If not set, the generator will look for a 'BadRequest' value in the enum.
        /// </summary>
        public string? BadRequestValue {{ get; set; }}

        // <summary>
        // Specifies whether to generate an 
        // </summary>
        public bool IncludeActionResultMapper {{ get; set; }}
    }}
}}";
    }
}