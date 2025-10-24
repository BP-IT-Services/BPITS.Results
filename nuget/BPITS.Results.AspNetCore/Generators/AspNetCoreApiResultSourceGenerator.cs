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

        // Generate filter for automatic ApiResult handling
        var filterSource = GenerateApiResultFilter(enumName, enumNamespace);
        context.AddSource($"ApiResultFilter_{enumName}.g.cs",
            SourceText.From(filterSource, Encoding.UTF8));
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using BPITS.Results;
using BPITS.Results.AspNetCore;
using BPITS.Results.AspNetCore.Mappers;
using BPITS.Results.AspNetCore.Filters;
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

        /// <summary>
        /// Registers the ApiResultFilter for {enumName} enum, enabling automatic status code mapping for ApiResult responses.
        /// Note: You still need to add the filter to MvcOptions using Add{enumName}ApiResultFilter() extension method.
        /// </summary>
        public static IServiceCollection Add{enumName}ApiResultFilter(this IServiceCollection services)
        {{
            services.AddSingleton<{enumName}ApiResultFilter>();
            return services;
        }}

        /// <summary>
        /// Registers the ActionResultMapper and ApiResultFilter for {enumName} enum.
        /// This provides comprehensive support for ApiResult handling in ASP.NET Core applications.
        /// Note: You still need to add the filter to MvcOptions using Add{enumName}ApiResultFilter() extension method.
        /// </summary>
        public static IServiceCollection Add{enumName}ApiResultSupport(this IServiceCollection services)
        {{
            services.Add{enumName}ActionResultMapper();
            services.Add{enumName}ApiResultFilter();
            return services;
        }}
    }}

    public static class MvcOptionsExtensions
    {{
        /// <summary>
        /// Adds the {enumName}ApiResultFilter to the MVC filters collection.
        /// This filter automatically sets HTTP status codes for ApiResult responses based on the mapper.
        /// </summary>
        public static MvcOptions Add{enumName}ApiResultFilter(this MvcOptions options)
        {{
            options.Filters.AddService<{enumName}ApiResultFilter>();
            return options;
        }}
    }}
}}";
    }

    private static string GenerateApiResultFilter(string enumName, string enumNamespace)
    {
        return $@"#nullable enable
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BPITS.Results;
using BPITS.Results.AspNetCore.Mappers;
using {enumNamespace};

namespace BPITS.Results.AspNetCore.Filters
{{
    /// <summary>
    /// Result filter that automatically converts ApiResult responses to proper HTTP responses with appropriate status codes.
    /// This filter intercepts ObjectResult instances containing ApiResult and sets the correct HTTP status code using the registered IActionResultMapper.
    /// </summary>
    public class {enumName}ApiResultFilter : IResultFilter
    {{
        private readonly IActionResultMapper<{enumName}> _mapper;

        public {enumName}ApiResultFilter(IActionResultMapper<{enumName}> mapper)
        {{
            _mapper = mapper;
        }}

        public void OnResultExecuting(ResultExecutingContext context)
        {{
            if (context.Result is ObjectResult objectResult)
            {{
                // Check if the value is an ApiResult or ApiResult<T>
                var value = objectResult.Value;
                if (value != null)
                {{
                    var valueType = value.GetType();

                    // Check for ApiResult (non-generic)
                    if (valueType.Name == ""ApiResult"" && !valueType.IsGenericType)
                    {{
                        var apiResult = value as dynamic;
                        if (apiResult != null)
                        {{
                            var statusCode = _mapper.MapStatusCode(apiResult.StatusCode);
                            objectResult.StatusCode = (int)statusCode;
                        }}
                    }}
                    // Check for ApiResult<T> (generic)
                    else if (valueType.IsGenericType && valueType.Name.StartsWith(""ApiResult""))
                    {{
                        var apiResult = value as dynamic;
                        if (apiResult != null)
                        {{
                            var statusCode = _mapper.MapStatusCode(apiResult.StatusCode);
                            objectResult.StatusCode = (int)statusCode;
                        }}
                    }}
                }}
            }}
        }}

        public void OnResultExecuted(ResultExecutedContext context)
        {{
            // No action needed after result execution
        }}
    }}
}}";
    }
}