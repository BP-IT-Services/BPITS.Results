using System;
using System.Net;

namespace BPITS.Results.AspNetCore.Abstractions;

/// <summary>
/// Attribute to specify the HTTP status code mapping for an enum value.
/// Use this on enum members to provide explicit status code mappings.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class HttpStatusCodeAttribute : Attribute
{
    public HttpStatusCode StatusCode { get; }

    public HttpStatusCodeAttribute(HttpStatusCode statusCode)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCodeAttribute(int statusCode)
    {
        StatusCode = (HttpStatusCode)statusCode;
    }
}