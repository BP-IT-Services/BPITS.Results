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
        var enumNamespace = enumSymbol.ContainingNamespace.ToDisplayString();
        var enumName = enumSymbol.Name;

        // Find the Ok value (required)
        if (!EnumFinder.Validate(enumSymbol, context))
            return;

        // Generate the primary ActionResultMapper based on HttpStatusCode attributes
        var mapperSource = GenerateActionResultMapper(enumName, enumNamespace, enumSymbol);
        context.AddSource($"ActionResultMapper_{enumName}.g.cs", SourceText.From(mapperSource, Encoding.UTF8));

        // Generate the AspNetCore-specific ApiResult that implements IActionResult
        var apiResultSource = GenerateAspNetCoreApiResult(enumName, enumNamespace);
        context.AddSource($"AspNetCoreApiResult_{enumName}.g.cs", SourceText.From(apiResultSource, Encoding.UTF8));

        // Generate DI extension methods
        var diExtensionsSource = GenerateDIExtensions(enumName, enumNamespace);
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
using BPITS.Results;
using {enumNamespace};

namespace BPITS.Results.AspNetCore.Mappers
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
                _ => HttpStatusCode.InternalServerError
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

    private static string GenerateAspNetCoreApiResult(string enumName, string enumNamespace)
    {
        return $@"#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using BPITS.Results;
using BPITS.Results.AspNetCore;
using BPITS.Results.AspNetCore.Mappers;
using {enumNamespace};

namespace BPITS.Results.AspNetCore
{{
    public static class ApiResultHttpStatusCodeMapperExtensions
    {{
        public static AspNetCoreApiResult<T> ToActionResult<T>(this ApiResult<T> apiResult, ActionContext context)
        {{
            return new AspNetCoreApiResult<T>(apiResult, context);
        }}

        public static AspNetCoreApiResult<object> ToActionResult(this ApiResult apiResult, ActionContext context)
        {{
            return new AspNetCoreApiResult<object>(apiResult, context);
        }}
    }}

    public class AspNetCoreApiResult<TValue> : IActionResult
    {{
        private readonly ApiResult<TValue> _apiResult;
        private readonly ActionContext _context;

        public AspNetCoreApiResult(ApiResult<TValue> apiResult, ActionContext context)
        {{
            _apiResult = apiResult;
            _context = context;
        }}

        public async Task ExecuteResultAsync(ActionContext context)
        {{
            var mapper = context.HttpContext.RequestServices.GetService<IActionResultMapper<{enumName}>>()
                ?? new {enumName}ActionResultMapper();

            var statusCode = mapper.MapStatusCode(_apiResult.StatusCode);
            var objectResult = new ObjectResult(_apiResult)
            {{
                StatusCode = (int)statusCode
            }};

            await objectResult.ExecuteResultAsync(context);
        }}
    }}
}}";
    }

    private static string GenerateDIExtensions(string enumName, string enumNamespace)
    {
        return $@"#nullable enable
using Microsoft.Extensions.DependencyInjection;
using BPITS.Results;
using BPITS.Results.AspNetCore;
using BPITS.Results.AspNetCore.Mappers;
using {enumNamespace};

namespace BPITS.Results.AspNetCore.Extensions
{{
    public static class ServiceCollectionExtensions
    {{
        /// <summary>
        /// Registers the ActionResultMapper for {enumName} enum, enabling ApiResult&lt;{enumName}&gt; to work with ASP.NET Core IActionResult.
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