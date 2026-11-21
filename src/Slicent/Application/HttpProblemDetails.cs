using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Slicent.Application;

/// <summary>
/// Standard error response returned when a request cannot be processed successfully.
/// It follows the RFC 7807 Problem Details for HTTP APIs specification and may include additional fields
/// to help with diagnostics and validation errors.
/// </summary>
public sealed class HttpProblemDetails
    : ProblemDetails
{
    /// <summary>
    /// Identifier of the current request trace.
    /// Can be provided when contacting support to help locate the request in server logs.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Identifier of the distributed trace activity associated with the request.
    /// Useful for correlating this error across multiple services in a distributed system.
    /// </summary>
    public string? ActivityTraceId { get; init; }

    /// <summary>
    /// Collection of validation or processing errors grouped by field or parameter name.
    /// Each entry contains one or more error codes describing why the value is invalid.
    /// </summary>
    // Keep the init accessor public: System.Text.Json populates this property when clients and tests
    // deserialize the response. Making it private turns the property into read-only outside Slicent.
    public Dictionary<string, string[]>? Errors { get; init; }

    public static HttpProblemDetails NotFound(
        HttpContext ctx,
        string title,
        string? detail = null)
        => Create(ctx, StatusCodes.Status404NotFound, title, detail);

    public static HttpProblemDetails Conflict(
        HttpContext ctx,
        string title,
        string? detail = null)
        => Create(ctx, StatusCodes.Status409Conflict, title, detail);

    public static HttpProblemDetails UnprocessableEntity(
        HttpContext ctx,
        string title,
        string? detail = null)
        => Create(ctx, StatusCodes.Status422UnprocessableEntity, title, detail);

    internal static HttpProblemDetails FromValidation(
        HttpContext ctx,
        HttpValidationProblemDetails vpd,
        string title = "validation_failed",
        string? detail = "One or more validation errors occurred.")
        => Create(
            ctx,
            StatusCodes.Status400BadRequest,
            title,
            detail,
            vpd.Errors.ToDictionary(error => ToCamelCase(error.Key), error => error.Value));

    internal static HttpProblemDetails FromUnauthorized(
        HttpContext ctx,
        string title = "unauthorized",
        string? detail = "Missing or invalid bearer token.")
        => Create(ctx, StatusCodes.Status401Unauthorized, title, detail);

    internal static HttpProblemDetails FromForbidden(
        HttpContext ctx,
        string title = "forbidden",
        string? detail = "You are not allowed to access this resource.")
        => Create(ctx, StatusCodes.Status403Forbidden, title, detail);

    private static HttpProblemDetails Create(
        HttpContext ctx,
        int status,
        string title,
        string? detail = null,
        Dictionary<string, string[]>? errors = null)
        => new()
        {
            Status = status,
            Type = TypeFor(status),
            Title = title,
            Detail = detail,
            Instance = ctx.Request.Path,
            TraceId = ctx.TraceIdentifier,
            ActivityTraceId = System.Diagnostics.Activity.Current?.TraceId.ToString(),
            Errors = errors
        };

    private static string TypeFor(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
        StatusCodes.Status401Unauthorized => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
        StatusCodes.Status403Forbidden => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4",
        StatusCodes.Status404NotFound => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
        StatusCodes.Status409Conflict => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
        StatusCodes.Status422UnprocessableEntity => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.21",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported problem status code.")
    };

    private static string ToCamelCase(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}
