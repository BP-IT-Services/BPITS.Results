using System.Text;
using BPITS.Results.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BPITS.Results.Generators;

[Generator]
public class ServiceResultSourceGenerator : IIncrementalGenerator
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

    private static void Execute(INamedTypeSymbol enumSymbol, SourceProductionContext context)
    {
        var enumNamespace = enumSymbol.ContainingNamespace.ToDisplayString();
        var enumName = enumSymbol.Name;

        // Find the Ok value (required)
        if (!EnumFinder.Validate(enumSymbol, context))
            return;

        // Generate the ServiceResult classes
        var serviceResultSource = GenerateServiceResultClasses(Constants.ResultNamespace, enumName, enumNamespace);
        context.AddSource($"ServiceResult_{enumName}.g.cs", SourceText.From(serviceResultSource, Encoding.UTF8));
    }

    private static string GenerateServiceResultClasses(string namespaceName, string enumName, string enumNamespace)
    {
        return $@"#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using {enumNamespace};

namespace {namespaceName}
{{
    public interface IServiceResult<T>
    {{
        {enumName} StatusCode {{ get; }}
        T? Value {{ get; }}
        Exception? InnerException {{ get; }}
        string? ErrorMessage {{ get; }}
        Dictionary<string, string[]>? ErrorDetails {{ get; }}
        bool IsSuccess {{ get; }}

        T Get();
        bool TryGet([MaybeNullWhen(false)] out T value);
    }}

    public abstract record BaseServiceResult<T>(
        {enumName} StatusCode,
        T? Value,
        Exception? InnerException = null,
        string? ErrorMessage = null,
        Dictionary<string, string[]>? ErrorDetails = null) : IServiceResult<T>
    {{
        /// <summary>
        /// Indicates whether the StatusCode is OK. 
        /// </summary>
        public bool IsSuccess => StatusCode == {enumName}.Ok;

        /// <summary>
        /// Indicates whether StatusCode is not OK.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Returns a value that is never null.
        /// An exception will be thrown if the value is null.
        /// </summary>
        /// <exception cref=""ArgumentNullException""></exception>
        public T Get()
        {{
            return Value ?? throw new ArgumentNullException(nameof(Value));
        }}

        /// <summary>
        /// Populates the provided variable with the ServiceResult value
        /// and returns false if the value is null, or true otherwise.
        /// </summary>
        /// <param name=""value""></param>
        public bool TryGet([MaybeNullWhen(false)] out T value)
        {{
            value = Value ?? default!;
            return Value is not null;
        }}
    }}

    public record ServiceResult(
        {enumName} StatusCode,
        object? Value,
        Exception? InnerException = null,
        string? ErrorMessage = null,
        Dictionary<string, string[]>? ErrorDetails = null)
        : BaseServiceResult<object>(StatusCode, Value, InnerException, ErrorMessage, ErrorDetails)
    {{
        public static implicit operator ServiceResult(ServiceResult<object> result) => new(
            StatusCode: result.StatusCode,
            Value: result.Value,
            InnerException: result.InnerException,
            ErrorMessage: result.ErrorMessage,
            ErrorDetails: result.ErrorDetails);

        public static implicit operator ServiceResult<object>(ServiceResult result) => new(
            StatusCode: result.StatusCode,
            Value: result.Value,
            InnerException: result.InnerException,
            ErrorMessage: result.ErrorMessage,
            ErrorDetails: result.ErrorDetails);

        /// <summary>
        /// Creates a ServiceResult of a new type and value based on the lambda provided.
        /// </summary>
        /// <typeparam name=""TAlt"">The type of the result after conversion.</typeparam>
        /// <param name=""mapFunc"">Function to determine the type and value of the new ServiceResult</param>
        /// <returns>A ServiceResult of the new type after applying the mapping function.</returns>
        public ServiceResult<TAlt> MapValue<TAlt>(Func<TAlt?> mapFunc)
        {{
            var mappedValue = mapFunc();
            return new ServiceResult<TAlt>(
                StatusCode: StatusCode,
                Value: mappedValue,
                ErrorMessage: ErrorMessage,
                InnerException: InnerException,
                ErrorDetails: ErrorDetails
            );
        }}

        /// <summary>
        /// Creates a new ServiceResult using the specified type and value.
        /// Optionally, the error message and status code can be overriden.
        /// </summary>
        /// <param name=""value""></param>
        /// <param name=""errorMessage""></param>
        /// <param name=""errorDetails""></param>
        /// <param name=""statusCode""></param>
        /// <typeparam name=""TAlt""></typeparam>
        public ServiceResult<TAlt> PassThroughFail<TAlt>(
            TAlt? value = default,
            string? errorMessage = null,
            Dictionary<string, string[]>? errorDetails = null,
            {enumName}? statusCode = null)
        {{
            return FailureFromServiceResult((ServiceResult<object>)this, value, errorMessage, statusCode, errorDetails);
        }}

        /// <summary>
        /// Create a typeless/empty successful ServiceResult, with a status code and an optional value.
        /// </summary>
        /// <returns>A ServiceResult with a status code of Ok and the specified value.</returns>
        public static ServiceResult Success() => new(
            StatusCode: {enumName}.Ok,
            Value: null);

        /// <summary>
        /// Create a successful ServiceResult, with the specified type, a status code and an optional value.
        /// </summary>
        /// <param name=""value""></param>
        /// <typeparam name=""T""></typeparam>
        /// <returns>A ServiceResult with a status code of Ok and the specified value.</returns>
        public static ServiceResult<T> Success<T>(T? value) => new(
            StatusCode: {enumName}.Ok,
            Value: value);

        /// <summary>
        /// Create a typeless/empty failure ServiceResult, with an error message,
        /// status code and error details.
        /// </summary>
        /// <param name=""errorMessage""></param>
        /// <param name=""statusCode""></param>
        /// <param name=""errorDetails""></param>
        public static ServiceResult Failure(
            string errorMessage,
            {enumName} statusCode = default,
            Dictionary<string, string[]>? errorDetails = null) =>
            Failure(null, errorMessage, statusCode, errorDetails);

        /// <summary>
        /// Create a failure ServiceResult, with the specified type, an error message,
        /// status code and value.
        /// </summary>
        /// <param name=""errorMessage""></param>
        /// <param name=""statusCode""></param>
        /// <param name=""errorDetails""></param>
        /// <param name=""value""></param>
        public static ServiceResult<T> Failure<T>(
            string errorMessage,
            {enumName} statusCode = default,
            Dictionary<string, string[]>? errorDetails = null,
            T? value = default) =>
            Failure(null, errorMessage, statusCode, value, errorDetails);

        /// <summary>
        /// Create a failure ServiceResult, with the specified type, an exception,
        /// error message, status code and value.
        /// </summary>
        /// <param name=""exception""></param>
        /// <param name=""errorMessage""></param>
        /// <param name=""statusCode""></param>
        /// <param name=""value""></param>
        /// <param name=""errorDetails""></param>
        /// <typeparam name=""T""></typeparam>
        public static ServiceResult<T> Failure<T>(
            Exception? exception = null,
            string? errorMessage = null,
            {enumName} statusCode = default,
            T? value = default,
            Dictionary<string, string[]>? errorDetails = null)
            => new(
                StatusCode: statusCode,
                Value: value,
                InnerException: exception,
                ErrorMessage: errorMessage,
                ErrorDetails: errorDetails
            );

        /// <summary>
        /// Create a typeless/empty failure ServiceResult, with an exception, error message,
        /// status code and error details.
        /// </summary>
        /// <param name=""exception""></param>
        /// <param name=""errorMessage""></param>
        /// <param name=""statusCode""></param>
        /// <param name=""errorDetails""></param>
        public static ServiceResult Failure(
            Exception? exception = null,
            string? errorMessage = null,
            {enumName} statusCode = default,
            Dictionary<string, string[]>? errorDetails = null)
            => new(
                StatusCode: statusCode,
                Value: null,
                InnerException: exception,
                ErrorMessage: errorMessage,
                ErrorDetails: errorDetails
            );

        /// <summary>
        /// Create a failure typeless/empty ServiceResult from an existing typed ServiceResult.
        /// An error message and/or status code can be specified to override the underlying ServiceResult's properties.
        /// </summary>
        /// <param name=""serviceResult"">ServiceResult to be ""copied""</param>
        /// <param name=""errorMessage"">Override the underlying ServiceResult's message</param>
        /// <param name=""statusCode"">Override the underlying ServiceResult's status code</param>
        public static ServiceResult FailureFromServiceResult<TAlt>(
            ServiceResult<TAlt> serviceResult,
            string? errorMessage = null,
            {enumName}? statusCode = null)
            => Failure<object>(
                exception: serviceResult.InnerException ?? null,
                errorMessage: errorMessage ?? serviceResult.ErrorMessage,
                statusCode: statusCode ?? serviceResult.StatusCode,
                errorDetails: serviceResult.ErrorDetails);

        /// <summary>
        /// Create a failure ServiceResult from an existing ServiceResult but using a new value.
        /// An error message and/or status code can be specified to override the underlying ServiceResult's properties.
        /// </summary>
        /// <param name=""serviceResult"">ServiceResult to be ""copied""</param>
        /// <param name=""value"">Value of the newly-created ServiceResult</param>
        /// <param name=""errorMessage"">Override the underlying ServiceResult's message</param>
        /// <param name=""errorDetails""></param>
        /// <param name=""statusCode"">Override the underlying ServiceResult's status code</param>
        /// <typeparam name=""TAlt"">Type of the input ServiceResult</typeparam>
        /// <typeparam name=""T"">Type of the output/return ServiceResult</typeparam>
        public static ServiceResult<T> FailureFromServiceResult<TAlt, T>(
            ServiceResult<TAlt> serviceResult,
            T? value = default,
            string? errorMessage = null,
            {enumName}? statusCode = null,
            Dictionary<string, string[]>? errorDetails = null)
            => Failure(
                exception: serviceResult.InnerException ?? null,
                errorMessage: errorMessage ?? serviceResult.ErrorMessage,
                statusCode: statusCode ?? serviceResult.StatusCode,
                value: value,
                errorDetails: errorDetails ?? serviceResult.ErrorDetails);

        public static ServiceResult<T> ValidationFailure<T>(
            string errorMessage,
            Dictionary<string, string[]>? errorDetails = null) =>
            Failure(null, errorMessage, {enumName}.BadRequest, default(T), errorDetails);
        
        public static ServiceResult<T> ValidationFailure<T>(
            string errorDetailsPropertyKey,
            string errorDetailsPropertyError)
        {{
            return Failure<T>(
                exception: null, 
                errorMessage: errorDetailsPropertyError, 
                statusCode: {enumName}.BadRequest, 
                value: default,
                errorDetails: new Dictionary<string, string[]> {{ {{ errorDetailsPropertyKey, new[] {{ errorDetailsPropertyError }} }} }});
        }}

        public static ServiceResult ValidationFailure(
            string errorMessage,
            Dictionary<string, string[]>? errorDetails = null) =>
            Failure<object>(null, errorMessage, {enumName}.BadRequest, null, errorDetails);

        public static ServiceResult ValidationFailure(
            string errorDetailsPropertyKey,
            string errorDetailsPropertyError)
        {{
            return Failure<object>(
                exception: null, 
                errorMessage: errorDetailsPropertyError, 
                statusCode: {enumName}.BadRequest, 
                value: null,
                errorDetails: new Dictionary<string, string[]> {{ {{ errorDetailsPropertyKey, new[] {{ errorDetailsPropertyError }} }} }});
        }}
    }}

    public record ServiceResult<T>(
        {enumName} StatusCode,
        T? Value,
        Exception? InnerException = null,
        string? ErrorMessage = null,
        Dictionary<string, string[]>? ErrorDetails = null
    ) : BaseServiceResult<T>(StatusCode, Value, InnerException, ErrorMessage, ErrorDetails)
    {{
        public static implicit operator ServiceResult<T>(T? value) => ServiceResult.Success(value);

        /// <summary>
        /// Creates a new typeless/empty ServiceResult using the specified type.
        /// Optionally, the error message and status code can be overriden.
        /// </summary>
        /// <param name=""errorMessage""></param>
        /// <param name=""errorDetails""></param>
        /// <param name=""statusCode""></param>
        public ServiceResult PassThroughFail(
            string? errorMessage = null,
            {enumName}? statusCode = null,
            Dictionary<string, string[]>? errorDetails = null)
        {{
            return ServiceResult.FailureFromServiceResult<T, object>(this, null, errorMessage, statusCode, errorDetails);
        }}

        /// <summary>
        /// Creates a new ServiceResult using the specified type and value.
        /// Optionally, the error message and status code can be overriden.
        /// </summary>
        /// <param name=""value""></param>
        /// <param name=""errorMessage""></param>
        /// <param name=""errorDetails""></param>
        /// <param name=""statusCode""></param>
        /// <typeparam name=""TAlt""></typeparam>
        public ServiceResult<TAlt> PassThroughFail<TAlt>(
            TAlt? value = default,
            string? errorMessage = null,
            {enumName}? statusCode = null,
            Dictionary<string, string[]>? errorDetails = null)
        {{
            return ServiceResult.FailureFromServiceResult(this, value, errorMessage, statusCode, errorDetails);
        }}

        /// <summary>
        /// Converts the current value using the specified mapping function **if the value is not null**,
        /// and returns a new ServiceResult of the specified type. If the value is null, the resulting value
        /// will be the default value for the specified type.
        /// </summary>
        /// <typeparam name=""TAlt"">The type of the result after conversion.</typeparam>
        /// <param name=""whenValueNotNullFunc"">The function to map the current value to the new type if it is not null.</param>
        /// <returns>An ApiResult of the new type after applying the mapping function, or the default value if the current value is null.</returns>
        public ServiceResult<TAlt> MapValueWhenNotNull<TAlt>(Func<T, TAlt?> whenValueNotNullFunc)
        {{
            return MapValue(whenValueNotNullFunc, _ => default);
        }}

        /// <summary>
        /// Converts the current value using the specified mapping function, 
        /// regardless of whether the value is null or not, and returns a new ServiceResult of the specified type.
        /// </summary>
        /// <typeparam name=""TAlt"">The type of the result after conversion.</typeparam>
        /// <param name=""mapFunc"">The function to map the current value to the new type.</param>
        /// <returns>An ApiResult of the new type after applying the mapping function.</returns>
        public ServiceResult<TAlt> MapValue<TAlt>(Func<T?, TAlt?> mapFunc)
        {{
            return MapValue(mapFunc, mapFunc);
        }}

        /// <summary>
        /// Converts the current value using the specified functions based on whether the value is null or not,
        /// and returns a new ServiceResult of the specified type.
        /// </summary>
        /// <typeparam name=""TAlt"">The type of the result after conversion.</typeparam>
        /// <param name=""whenValueNotNullFunc"">The function to map the current value to the new type if it is not null.</param>
        /// <param name=""whenValueNullFunc"">The function to map the current value to the new type if it is null.</param>
        /// <returns>A ServiceResult of the new type after applying the mapping function.</returns>
        public ServiceResult<TAlt> MapValue<TAlt>(
            Func<T, TAlt?> whenValueNotNullFunc,
            Func<T?, TAlt?> whenValueNullFunc)
        {{
            var mappedValue = Value is not null
                ? whenValueNotNullFunc(Value)
                : whenValueNullFunc(Value);

            return new ServiceResult<TAlt>(
                StatusCode: StatusCode,
                Value: mappedValue,
                ErrorMessage: ErrorMessage,
                InnerException: InnerException,
                ErrorDetails: ErrorDetails
            );
        }}
    }}
}}";
    }
}