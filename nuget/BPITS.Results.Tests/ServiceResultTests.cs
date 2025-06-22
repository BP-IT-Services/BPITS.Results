namespace BPITS.Results.Tests;

public class ServiceResultTests
{
    [Fact]
    public void ValidationFailure_ReturnsExpected()
    {
        var result = ServiceResult.ValidationFailure("TestKey", "Test Error");    
    }
}