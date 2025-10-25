using System;
using System.Net;

namespace BPITS.Results.AspNetCore.Abstractions;

public interface IActionResultMapper<in TEnum> where TEnum : Enum
{
    HttpStatusCode MapStatusCode(TEnum statusCode);
}