using System.Net;
using BPITS.Results.Abstractions;
using BPITS.Results.AspNetCore.Abstractions;

namespace BPITS.Results.AspNetCore.Tests.Scaffolding;

[GenerateApiResult(
    DefaultFailureValue = nameof(GenericFailure),
    BadRequestValue = nameof(BadRequest)
)]
[GenerateServiceResult(
    DefaultFailureValue = nameof(GenericFailure),
    BadRequestValue = nameof(BadRequest)
)]
[EnableApiResultMapping]
public enum TestStatusCode
{
    [HttpStatusCode(HttpStatusCode.InternalServerError)]
    GenericFailure = 0,
    
    [HttpStatusCode(HttpStatusCode.OK)]
    Ok = 1,
    
    [HttpStatusCode(HttpStatusCode.BadRequest)]
    BadRequest = 2,
    
    [HttpStatusCode(HttpStatusCode.Forbidden)]
    InsufficientPermissions = 3,
    
    [HttpStatusCode(HttpStatusCode.Unauthorized)]
    AuthenticationTokenInvalid = 4,
    
    [HttpStatusCode(HttpStatusCode.Unauthorized)]
    InvalidCredentials = 5,
    
    [HttpStatusCode(HttpStatusCode.NotFound)]
    ResourceNotFound = 6,
    
    [HttpStatusCode(HttpStatusCode.Conflict)]
    ResourceAlreadyExists = 7,
    
    [HttpStatusCode(HttpStatusCode.Gone)]
    ResourceExpired = 8,
    
    [HttpStatusCode(HttpStatusCode.ServiceUnavailable)]
    ResourceDisabled = 9,
    
    [HttpStatusCode(HttpStatusCode.ServiceUnavailable)]
    FunctionalityDisabled = 10,
    
    [HttpStatusCode(HttpStatusCode.BadGateway)]
    ExternalServiceFailure = 11,
}