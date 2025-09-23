using System.Net;

namespace BPITS.Results;

public interface IActionResultMapper
{
    public static HttpStatusCode MapStatusCode(ApiResult result);
}