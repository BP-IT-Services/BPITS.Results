using System.Text;
using BPITS.Results.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BPITS.Results.Generators;

[Generator]
public class ApiResultSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all enums with the ResultStatusCode attribute
        var enumDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => EnumFinder.IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => EnumFinder.GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // Generate the ServiceResult and ApiResult classes for each enum
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

        // Generate the ServiceResult classes
        var apiResultSource = GenerateApiResultClasses(Constants.ResultNamespace, enumName, enumNamespace);
        context.AddSource($"ApiResult_{enumName}.g.cs", SourceText.From(apiResultSource, Encoding.UTF8));
    }

    private static string GenerateApiResultClasses(string namespaceName, string enumName, string enumNamespace)
    {
        var actionResultMapperUsings = "";
        var actionResultMapperClassInheritance = "";
        
        return $@"#nullable enable
using System;
using System.Collections.Generic;
using {enumNamespace};
{actionResultMapperUsings}

namespace {namespaceName}
{{
    public partial record ApiResult(
        {enumName} StatusCode,
        object? Value,
        string? ErrorMessage = null,
        Dictionary<string, string[]>? ErrorDetails = null
    ) {actionResultMapperClassInheritance}
    {{
        public static implicit operator ApiResult(ApiResult<object> objResult) => new ApiResult(
            StatusCode: objResult.StatusCode,
            ErrorMessage: objResult.ErrorMessage,
            Value: objResult.Value);
        
        public static implicit operator ApiResult(ServiceResult value) => FromServiceResult(value);

        
        /// <summary>
        /// Creates an ApiResult with an ""Ok"" status code and no value.
        /// </summary>
        public static ApiResult Success()
        {{
            return new ApiResult({enumName}.Ok, null);
        }}
        
        /// <summary>
        /// Creates an ApiResult with an ""Ok"" status code and the provided value.
        /// </summary>
        public static ApiResult<T> Success<T>(T? value)
        {{
            return new ApiResult<T>({enumName}.Ok, value);
        }}
        
        /// <summary>
        /// Creates an ApiResult with an error message, error status code, with no value.
        /// </summary>
        public static ApiResult Failure(
            string? errorMessage,
            {enumName} statusCode = default)
        {{
            return new ApiResult(statusCode, null, errorMessage);
        }}

        /// <summary>
        /// Creates an ApiResult with an error message, error status code, and optional value.
        /// </summary>
        public static ApiResult<T> Failure<T>(
            string? errorMessage,
            {enumName} statusCode = default,
            T? value = default)
        {{
            return new ApiResult<T>(statusCode, value, errorMessage);
        }}

        /// <summary>
        /// Creates an ApiResult with an error status code, and optional value.
        /// </summary>
        public static ApiResult<T> Failure<T>(
            {enumName} statusCode = default,
            T? value = default)
        {{
            return new ApiResult<T>(statusCode, value);
        }}
        
        /// <summary>
        /// Creates an ApiResult from a ServiceResult by copying its contents.
        /// If provided, the error message, status code, and value can be overriden with new values.
        /// </summary>
        public static ApiResult FromServiceResult(
            ServiceResult serviceResult,
            string? errorMessage = null,
            {enumName}? statusCode = null)
        {{
            return new ApiResult(
                StatusCode: statusCode ?? serviceResult.StatusCode,
                ErrorMessage: errorMessage ?? serviceResult.ErrorMessage,
                Value: serviceResult.Value,
                ErrorDetails: serviceResult.ErrorDetails);
        }}

        /// <summary>
        /// Creates an ApiResult from a ServiceResult by copying its contents.
        /// If provided, the error message, status code, and value can be overriden with new values.
        /// </summary>
        public static ApiResult<T> FromServiceResult<T>(
            ServiceResult<T> serviceResult,
            string? errorMessage = null,
            {enumName}? statusCode = null)
        {{
            return new ApiResult<T>(
                StatusCode: statusCode ?? serviceResult.StatusCode,
                ErrorMessage: errorMessage ?? serviceResult.ErrorMessage,
                Value: serviceResult.Value,
                ErrorDetails: serviceResult.ErrorDetails);
        }}
        
        [Obsolete(""Expected parameter of type ServiceResult<T> but received T instead."", error: true)]
        public static ApiResult<T> FromServiceResult<T>(
            T? _1,
            string? _2 = null,
            {enumName}? _3 = null)
        {{
            throw new InvalidOperationException(""FromServiceResult must be called with ServiceResult<T> not T."");
        }}
        
        /// <summary>
        /// Creates an ApiResult from a ServiceResult of a different type by copying its contents, except the value.
        /// To confirm with the new value type, it will be initialised to ""default"" if not specified.
        /// 
        /// If provided, the error message, status code, and value can be overriden with new values.
        /// </summary>
        public static ApiResult<T> FromServiceResult<T, TServiceResult>(
            ServiceResult<TServiceResult> serviceResult,
            string? errorMessage = null,
            {enumName}? statusCode = null,
            T? value = default)
        {{
            return new ApiResult<T>(
                StatusCode: statusCode ?? serviceResult.StatusCode,
                ErrorMessage: errorMessage ?? serviceResult.ErrorMessage,
                Value: value,
                ErrorDetails: serviceResult.ErrorDetails);
        }}
    }}

    public partial record ApiResult<T>(
        {enumName} StatusCode,
        T? Value,
        string? ErrorMessage = null,
        Dictionary<string, string[]>? ErrorDetails = null
    )
    {{
        public static implicit operator ApiResult<T>(T? value) => ApiResult.Success(value);
        public static implicit operator ApiResult<T>(ServiceResult<T> value) => ApiResult.FromServiceResult(value);
    }}
}}";
    }
}
