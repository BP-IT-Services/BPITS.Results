using System.Text;
using BPITS.Results.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BPITS.Results.AspNetCore.Generators;

[Generator]
public class AspNetCoreApiResultSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all enums with the ResultStatusCode attribute that include ActionResultMapper
        var enumDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => EnumFinder.IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => EnumFinder.GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null && m.IncludeActionResultMapper);

        // Generate the AspNetCore-specific ApiResult classes and mappers
        context.RegisterSourceOutput(enumDeclarations,
            static (spc, source) => Execute(source!, spc));
    }

    private static void Execute(ApiResultGeneratorArguments generatorArgs, SourceProductionContext context)
    {
        var enumSymbol = generatorArgs.NamedTypeSymbol;
        var hostNamespace = enumSymbol.ContainingNamespace.ToDisplayString();
        var enumName = enumSymbol.Name;

        // Find the Ok value (required)
        if (!EnumFinder.Validate(enumSymbol, context))
            return;

        // Generate the primary ActionResultMapper based on HttpStatusCode attributes
        var mapperSource = GenerateActionResultMapper(enumName, hostNamespace, enumSymbol);
        context.AddSource($"ActionResultMapper_{enumName}.g.cs", SourceText.From(mapperSource, Encoding.UTF8));

        // Generate partial classes that make ApiResult implement IActionResult
        var apiResultPartialSource = GenerateApiResultPartialClasses(enumName, hostNamespace);
        context.AddSource($"ApiResult_{enumName}_IActionResult.g.cs", SourceText.From(apiResultPartialSource, Encoding.UTF8));

        // Generate DI extension methods
        var diExtensionsSource = GenerateDependencyInjectionExtensions(enumName, hostNamespace);
        context.AddSource($"ServiceCollectionExtensions_{enumName}.g.cs",
            SourceText.From(diExtensionsSource, Encoding.UTF8));
    }

    private static string GenerateActionResultMapper(string enumName, string enumNamespace, INamedTypeSymbol enumSymbol)
    {
        var attributeMappings = GenerateAttributeMappings(enumSymbol);
        var hasAttributeMappings = !string.IsNullOrEmpty(attributeMappings);
        // if (!hasAttributeMappings)
        //     return string.Empty;

        // Generate attribute-based mapper as the primary implementation
        return $@"#nullable enable
using System.Net;
using BPITS.Results.AspNetCore;

namespace {enumNamespace}
{{
    /// <summary>
    /// ActionResultMapper for {enumName} using HttpStatusCode attributes.
    /// This mapper uses the [HttpStatusCode] attributes defined on enum values.
    /// </summary>
    public class {enumName}ActionResultMapper : IActionResultMapper<{enumName}>
    {{
        public HttpStatusCode MapStatusCode({enumName} statusCode)
        {{
            return statusCode switch
            {{
{attributeMappings}
                _ => HttpStatusCode.OK
            }};
        }}
    }}
}}";
    }

    private static string GenerateAttributeMappings(INamedTypeSymbol enumSymbol)
    {
        var mappings = new StringBuilder();
        bool hasAttributeMappings = false;

        foreach (var member in enumSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsStatic && member.HasConstantValue)
            {
                var httpStatusAttribute = member.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Name == "HttpStatusCodeAttribute");

                if (httpStatusAttribute != null && httpStatusAttribute.ConstructorArguments.Length > 0)
                {
                    hasAttributeMappings = true;
                    var statusCodeValue = httpStatusAttribute.ConstructorArguments[0].Value;
                    mappings.AppendLine(
                        $"                {enumSymbol.Name}.{member.Name} => (HttpStatusCode){statusCodeValue},");
                }
            }
        }

        return hasAttributeMappings ? mappings.ToString().TrimEnd() : string.Empty;
    }

    private static string GenerateApiResultPartialClasses(string enumName, string enumNamespace)
    {
        return $@"#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using {Constants.MainNamespace};
using {enumNamespace};

namespace BPITS.Results
{{
    /// <summary>
    /// Partial class that makes ApiResult implement IActionResult for ASP.NET Core integration.
    /// This allows ApiResult to be returned directly from controller actions.
    /// </summary>
    public partial record ApiResult : IActionResult
    {{
        public async Task ExecuteResultAsync(ActionContext context)
        {{
            var mapper = context.HttpContext.RequestServices.GetService<IActionResultMapper<{enumName}>>()
                ?? new {enumName}ActionResultMapper();

            var statusCode = mapper.MapStatusCode(this.StatusCode);
            var objectResult = new ObjectResult(this)
            {{
                StatusCode = (int)statusCode
            }};

            await objectResult.ExecuteResultAsync(context);
        }}
    }}

    /// <summary>
    /// Partial class that makes ApiResult&lt;T&gt; implement IActionResult for ASP.NET Core integration.
    /// This allows ApiResult&lt;T&gt; to be returned directly from controller actions.
    /// </summary>
    public partial record ApiResult<T> : IActionResult
    {{
        public async Task ExecuteResultAsync(ActionContext context)
        {{
            var mapper = context.HttpContext.RequestServices.GetService<IActionResultMapper<{enumName}>>()
                ?? new {enumName}ActionResultMapper();

            var statusCode = mapper.MapStatusCode(this.StatusCode);
            var objectResult = new ObjectResult(this)
            {{
                StatusCode = (int)statusCode
            }};

            await objectResult.ExecuteResultAsync(context);
        }}
    }}
}}";;
    }

    private static string GenerateDependencyInjectionExtensions(string enumName, string enumNamespace)
    {
        return $@"#nullable enable
using Microsoft.Extensions.DependencyInjection;
using {enumNamespace};

namespace BPITS.Results.AspNetCore
{{
    public static class ServiceCollectionExtensions
    {{
        /// <summary>
        /// Registers the ActionResultMapper for {enumName} enum, enabling ApiResult to automatically map status codes when returned from controller actions.
        /// This is optional - if not registered, the default mapper will be used.
        /// </summary>
        public static IServiceCollection Add{enumName}ActionResultMapper(this IServiceCollection services)
        {{
            services.AddSingleton<IActionResultMapper<{enumName}>, {enumName}ActionResultMapper>();
            return services;
        }}
    }}
}}";
    }
}