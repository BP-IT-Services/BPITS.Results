# Installation

This guide walks you through installing BPITS.Results in your .NET project.

## Prerequisites

- .NET 6.0 or later
- A C# project (library, console app, web API, etc.)

## Installing BPITS.Results

### For Basic Result Pattern Support

If you only need the core Result pattern without ASP.NET Core integration:

```bash
dotnet add package BPITS.Results
```

This will install:
- **BPITS.Results** - Core result types and source generation
- **BPITS.Results.Abstractions** - Core abstractions (automatically included as a dependency)

### For ASP.NET Core Integration

If you're building an ASP.NET Core application and want automatic HTTP status code mapping:

```bash
dotnet add package BPITS.Results
dotnet add package BPITS.Results.AspNetCore
```

This will install:
- **BPITS.Results** - Core result types
- **BPITS.Results.AspNetCore** - ASP.NET Core integration with IActionResult support
- **BPITS.Results.Abstractions** - Core abstractions (automatically included)
- **BPITS.Results.AspNetCore.Abstractions** - ASP.NET Core abstractions (automatically included)

## Package Manager Console (Visual Studio)

If you prefer using the Package Manager Console in Visual Studio:

```powershell
# Core package only
Install-Package BPITS.Results

# Core + ASP.NET Core integration
Install-Package BPITS.Results
Install-Package BPITS.Results.AspNetCore
```

## Manual Installation (.csproj)

You can also add the package references directly to your `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="BPITS.Results" Version="1.0.0" />
  <!-- Optional: Add for ASP.NET Core integration -->
  <PackageReference Include="BPITS.Results.AspNetCore" Version="1.0.0" />
</ItemGroup>
```

## Verifying Installation

After installation, verify that the packages are correctly installed:

```bash
dotnet list package
```

You should see the BPITS.Results packages in the output.

## Version Compatibility

| BPITS.Results Version | .NET Version | ASP.NET Core Version |
|-----------------------|--------------|----------------------|
| 1.0.x                 | .NET 6.0+    | 2.2.0+               |

## What Gets Installed?

### BPITS.Results Package

- Source generators for creating Result types from your enums
- `ServiceResult<T>` and non-generic `ServiceResult`
- `ApiResult<T>` and non-generic `ApiResult`
- Extension methods and helper utilities

### BPITS.Results.AspNetCore Package

- IActionResult implementation for ApiResult
- HTTP status code mapping infrastructure
- Source generator for creating action result mappers
- DI extension methods for registering mappers

## Next Steps

Now that you have BPITS.Results installed:

1. **[Quick Start Guide](quick-start.md)** - Build your first Result-based application
2. **[Core Concepts](core-concepts.md)** - Understand ServiceResult vs ApiResult
3. **[Working with Results](../guides/working-with-results.md)** - Learn common patterns

## Troubleshooting

### Source Generator Not Running

If the source generator isn't creating your Result types:

1. Clean and rebuild your solution:
   ```bash
   dotnet clean
   dotnet build
   ```

2. Check that your enum has the correct attributes:
   ```csharp
   [GenerateApiResult]  // or [GenerateServiceResult]
   public enum MyStatus { Ok = 0, Error = 500 }
   ```

3. Verify your project targets .NET 6.0 or later in your `.csproj`:
   ```xml
   <TargetFramework>net6.0</TargetFramework>
   ```

### Package Restore Issues

If you encounter package restore issues:

```bash
dotnet nuget locals all --clear
dotnet restore
```

## Need Help?

- Check the [GitHub Issues](https://github.com/BP-IT-Services/BPITS.Results/issues)
- Review the [documentation](../README.md)
- Open a [new issue](https://github.com/BP-IT-Services/BPITS.Results/issues/new) if you encounter problems
