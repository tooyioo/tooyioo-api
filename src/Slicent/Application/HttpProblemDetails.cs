
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
    public Dictionary<string, string[]>? Errors { get; init; }
    
    private const string BadRequestType = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1";
    private const string NotFoundType  = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5";
    private const string ConflictType  = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10";
    private const string UnprocessableEntityType = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.21";
    private const string UnauthorizedType = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2";
    private const string ForbiddenType    = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4";
    
    internal static HttpProblemDetails BadRequest(
        HttpContext ctx,
        string title,
        string? detail = null,
        IDictionary<string, string[]>? errors = null)
        => Create(
            ctx,
            status: StatusCodes.Status400BadRequest,
            type: BadRequestType,
            title: title,
            detail: detail,
            errors: errors is null ? null : ToCamelCaseKeys(errors));

    internal static HttpProblemDetails NotFound(
        HttpContext ctx,
        string title,
        string? detail = null)
        => Create(
            ctx,
            status: StatusCodes.Status404NotFound,
            type: NotFoundType,
            title: title,
            detail: detail,
            errors: null);
    
    internal static HttpProblemDetails Conflict(
        HttpContext ctx,
        string title,
        string? detail = null)
        => Create(
            ctx,
            status: StatusCodes.Status409Conflict,
            type: ConflictType,
            title: title,
            detail: detail,
            errors: null);

    internal static HttpProblemDetails UnprocessableEntity(
        HttpContext ctx,
        string title,
        string? detail = null)
        => Create(
            ctx,
            status: StatusCodes.Status422UnprocessableEntity,
            type: UnprocessableEntityType,
            title: title,
            detail: detail,
            errors: null);

    internal static HttpProblemDetails FromValidation(
        HttpContext ctx,
        HttpValidationProblemDetails vpd,
        string title = "validation_failed",
        string? detail = "One or more validation errors occurred.")
        => BadRequest(ctx, title, detail, vpd.Errors);
    
    internal static HttpProblemDetails FromUnauthorized(
        HttpContext ctx,
        string title = "unauthorized",
        string? detail = "Missing or invalid bearer token.")
        => Create(ctx, StatusCodes.Status401Unauthorized, UnauthorizedType, title, detail, errors: null);
    
    internal static HttpProblemDetails FromForbidden(
        HttpContext ctx,
        string title = "forbidden",
        string? detail = "You are not allowed to access this resource.")
        => Create(ctx, StatusCodes.Status403Forbidden, ForbiddenType, title, detail, errors: null);

    private static HttpProblemDetails Create(
        HttpContext ctx,
        int status,
        string type,
        string title,
        string? detail,
        Dictionary<string, string[]>? errors)
        => new()
        {
            Status = status,
            Type = type,
            Title = title,
            Detail = detail,
            Instance = ctx.Request.Path,
            TraceId = ctx.TraceIdentifier,
            ActivityTraceId = System.Diagnostics.Activity.Current?.TraceId.ToString(),
            Errors = errors
        };

    private static Dictionary<string, string[]> ToCamelCaseKeys(IDictionary<string, string[]> errors)
        => errors.ToDictionary(kvp => ToCamelCase(kvp.Key), kvp => kvp.Value);

    private static string ToCamelCase(string s)
    {
        if (string.IsNullOrEmpty(s) || char.IsLower(s[0]))
        {
            return s;
        }
        
        return char.ToLowerInvariant(s[0]) + s[1..];
    }
}
