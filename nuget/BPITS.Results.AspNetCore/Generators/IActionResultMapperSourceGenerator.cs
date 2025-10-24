using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BPITS.Results.AspNetCore.Generators;

[Generator]
public class ActionResultMapperSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Define the attribute that users will apply to their enum
        context.RegisterPostInitializationOutput(Execute);
    }

    private static void Execute(IncrementalGeneratorPostInitializationContext context)
    {
        var attributeSource = GenerateResultStatusCodeAttribute(Constants.MainNamespace);
        context.AddSource("IActionResultMapper.g.cs", SourceText.From(attributeSource, Encoding.UTF8));
    }

    private static string GenerateResultStatusCodeAttribute(string mapperNamespace)
    {
        return $@"#nullable enable
using System.Net;

namespace {mapperNamespace};

public interface IActionResultMapper<in TEnum> where TEnum : Enum
{{
    HttpStatusCode MapStatusCode(TEnum statusCode);
}}";
    }
}