using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BPITS.Results.Helpers;

public static class EnumFinder
{
    public static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is EnumDeclarationSyntax m && m.AttributeLists.Count > 0;

    public static INamedTypeSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var enumDeclarationSyntax = (EnumDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var enumSymbol = model.GetDeclaredSymbol(enumDeclarationSyntax) as INamedTypeSymbol;
            
        if (enumSymbol is null)
            return null;

        // Check if the enum has the ResultStatusCode attribute
        var hasAttribute = enumSymbol.GetAttributes()
            .Any(ad => ad.AttributeClass?.Name == "ResultStatusCodeAttribute" 
                       || ad.AttributeClass?.Name == "ResultStatusCode");

        return hasAttribute ? enumSymbol : null;
    }

    public static bool Validate(INamedTypeSymbol enumSymbol, SourceProductionContext context)
    {
        // Find the Ok value (required)
        var hasOkValue = enumSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(f => f.Name == "Ok");

        if (hasOkValue)
            return true;
        
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                "BPITSR001",
                "Missing Ok enum value",
                "The enum '{0}' must have an 'Ok' value to be used as a ResultStatusCode",
                "ResultSourceGenerator",
                DiagnosticSeverity.Error,
                true),
            enumSymbol.Locations.First(),
            enumSymbol.Name);
                
        context.ReportDiagnostic(diagnostic);
        return false;
    }
}