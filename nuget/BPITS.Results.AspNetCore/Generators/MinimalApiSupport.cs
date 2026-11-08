using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BPITS.Results.AspNetCore.Generators;

/// <summary>
/// What the consuming compilation can support for Minimal API integration.
/// </summary>
/// <remarks>
/// The generated code is compiled into the consumer's project, so emitting a reference to a type the consumer
/// doesn't have would break their build. That matters for class libraries that only reference the
/// netstandard ASP.NET Core packages, which predate <c>IResult</c> and static abstract interface members.
/// Kept as a small equatable value so the incremental pipeline doesn't regenerate on unrelated edits.
/// </remarks>
internal readonly struct MinimalApiSupport : IEquatable<MinimalApiSupport>
{
    public bool CanImplementResult { get; }
    public bool CanPopulateMetadata { get; }

    public MinimalApiSupport(bool canImplementResult, bool canPopulateMetadata)
    {
        CanImplementResult = canImplementResult;
        CanPopulateMetadata = canPopulateMetadata;
    }

    public static MinimalApiSupport Detect(Compilation compilation)
    {
        var result = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IResult");
        var jsonOptions = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.Json.JsonOptions");
        var options = compilation.GetTypeByMetadataName("Microsoft.Extensions.Options.IOptions`1");
        var jsonExtensions = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpResponseJsonExtensions");

        // The JsonTypeInfo overload (ASP.NET Core 7+) is what keeps serialization trim/AOT friendly.
        var hasTypeInfoWriter = jsonExtensions?
            .GetMembers("WriteAsJsonAsync")
            .OfType<IMethodSymbol>()
            .Any(m => m.Parameters.Any(p => p.Type is INamedTypeSymbol { Name: "JsonTypeInfo", IsGenericType: true }))
            ?? false;

        var canImplementResult = result is not null
            && jsonOptions is not null
            && options is not null
            && hasTypeInfoWriter;

        var metadataProvider = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.Metadata.IEndpointMetadataProvider");
        var producesMetadata = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.ProducesResponseTypeMetadata");

        // PopulateMetadata is a static abstract interface member, which needs C# 11.
        var supportsStaticAbstract = compilation is CSharpCompilation { LanguageVersion: >= LanguageVersion.CSharp11 };

        var canPopulateMetadata = canImplementResult
            && metadataProvider is not null
            && producesMetadata is { DeclaredAccessibility: Accessibility.Public }
            && supportsStaticAbstract;

        return new MinimalApiSupport(canImplementResult, canPopulateMetadata);
    }

    public bool Equals(MinimalApiSupport other)
        => CanImplementResult == other.CanImplementResult && CanPopulateMetadata == other.CanPopulateMetadata;

    public override bool Equals(object? obj) => obj is MinimalApiSupport other && Equals(other);

    public override int GetHashCode() => (CanImplementResult ? 1 : 0) | (CanPopulateMetadata ? 2 : 0);
}
