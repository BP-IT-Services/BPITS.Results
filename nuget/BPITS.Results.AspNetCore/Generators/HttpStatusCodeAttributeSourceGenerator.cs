using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BPITS.Results.AspNetCore.Generators;

[Generator]
public class HttpStatusCodeAttributeSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Define the attribute that users will apply to their enum
        context.RegisterPostInitializationOutput(Execute);
    }

    private static void Execute(IncrementalGeneratorPostInitializationContext context)
    {
        var attributeSource = GenerateResultStatusCodeAttribute(Constants.MainNamespace);
        context.AddSource("HttpStatusCodeAttribute.g.cs", SourceText.From(attributeSource, Encoding.UTF8));
    }

    private static string GenerateResultStatusCodeAttribute(string attributeNamespace)
    {
        return $@"#nullable enable
using System.Net;

namespace {attributeNamespace};

/// <summary>
/// Attribute to specify the HTTP status code mapping for an enum value.
/// Use this on enum members to provide explicit status code mappings.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class HttpStatusCodeAttribute : Attribute
{{
    public HttpStatusCode StatusCode {{ get; }}

    public HttpStatusCodeAttribute(HttpStatusCode statusCode)
    {{
        StatusCode = statusCode;
    }}

    public HttpStatusCodeAttribute(int statusCode)
    {{
        StatusCode = (HttpStatusCode)statusCode;
    }}
}}";
    }
}