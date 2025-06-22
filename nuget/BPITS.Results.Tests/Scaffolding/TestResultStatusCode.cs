namespace BPITS.Results.Tests.Scaffolding;

[ResultStatusCode]
public enum TestResultStatusCode
{
    GenericFailure = 0,
    Ok = 1,
    BadRequest = 2,
    InsufficientPermissions = 3,
    AuthenticationTokenInvalid = 4,
    InvalidCredentials = 5,
    ResourceNotFound = 6,
    ResourceAlreadyExists = 7,
    ResourceExpired = 8,
    ResourceDisabled = 9,
    FunctionalityDisabled = 10,
    ExternalServiceFailure = 11,
}