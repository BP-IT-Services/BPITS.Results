using BPITS.Results.Tests.Scaffolding;

namespace BPITS.Results.Tests;

public class ServiceResultTests
{
    [Fact]
    public void Success_WithNoValue_ReturnsExpected()
    {
        var result = ServiceResult.Success();
        Assert.Equal(TestResultStatusCode.Ok, result.StatusCode);
        Assert.Null(result.Value);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorDetails);
    }
    
    [Fact]
    public void Success_WithValue_ReturnsExpected()
    {
        var result = ServiceResult.Success(23);
        Assert.Equal(TestResultStatusCode.Ok, result.StatusCode);
        Assert.Equal(23, result.Value);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorDetails);
    }
    
    [Fact]
    public void ValidationFailure_ReturnsExpected()
    {
        var result = ServiceResult.ValidationFailure("TestKey", "Test Error");
        Assert.Equal(TestResultStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("TestKey", result.ErrorDetails?.First().Key);
        Assert.Contains("Test Error", result.ErrorDetails?.First().Value ?? []);
    }
    
    [Fact]
    public void MapValue_WhenNotNull_ReturnsExpected()
    {
        var nonDtoResult = ServiceResult.Success<ExampleUser>(new ExampleUser("Test", "Test"));
        var dtoResult = nonDtoResult.MapValue(e => e?.ToDto());
        
        Assert.Equal(TestResultStatusCode.Ok, dtoResult.StatusCode);
        Assert.IsType<ExampleUserDto>(dtoResult.Value);
        Assert.Equal("Test", dtoResult.Value?.Name);
    }
    
    [Fact]
    public void MapValue_WhenNull_ReturnsExpected()
    {
        var nonDtoResult = ServiceResult.Failure<ExampleUser>();
        var dtoResult = nonDtoResult.MapValue(_ => true, _ => false);
        
        Assert.Equal(TestResultStatusCode.GenericFailure, dtoResult.StatusCode);
        Assert.IsType<bool>(dtoResult.Value);
        Assert.False(dtoResult.Value);
    }
    
    [Fact]
    public void PassthroughFail_WithNoOverride_ReturnsExpected()
    {
        var nonDtoResult = ServiceResult.Failure<ExampleUser>("Disabled", TestResultStatusCode.FunctionalityDisabled);
        var passthroughResult = nonDtoResult.PassThroughFail();
        
        Assert.Equal(TestResultStatusCode.FunctionalityDisabled, passthroughResult.StatusCode);
        Assert.Equal("Disabled", passthroughResult.ErrorMessage);
    }
    
    [Fact]
    public void PassthroughFail_WithOverride_ReturnsExpected()
    {
        var nonDtoResult = ServiceResult.Failure<ExampleUser>("Disabled", TestResultStatusCode.FunctionalityDisabled);
        var passthroughResult = nonDtoResult.PassThroughFail("New error message", TestResultStatusCode.AuthenticationTokenInvalid);
        
        Assert.Equal(TestResultStatusCode.AuthenticationTokenInvalid, passthroughResult.StatusCode);
        Assert.Equal("New error message", passthroughResult.ErrorMessage);
    }
}

public record ExampleUser(string Name, string Password);

public record ExampleUserDto(string Name);

public static class ExampleUserExtensions
{
    public static ExampleUserDto ToDto(this ExampleUser user) => new ExampleUserDto(user.Name);
}