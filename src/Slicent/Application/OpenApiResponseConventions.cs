using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Slicent.Application;

/// <summary>
/// OpenAPI response conventions applied by the endpoint mapping helpers, so a slice cannot forget them
/// or pair them with the wrong verb.
/// </summary>
internal static class OpenApiResponseConventions
{
    private const string Json = "application/json";
    private const string ProblemJson = "application/problem+json";

    public static RouteHandlerBuilder WithCommandResponses<TResponse>(
        this RouteHandlerBuilder builder,
        int successStatus)
        where TResponse : class
        => builder
            .Produces<TResponse>(successStatus, Json)
            .Produces<HttpProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status403Forbidden, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemJson);

    public static RouteHandlerBuilder WithQueryResponses<TResponse>(this RouteHandlerBuilder builder)
        where TResponse : class
        => builder
            .Produces<TResponse>(StatusCodes.Status200OK, Json)
            .Produces<HttpProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status403Forbidden, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status404NotFound, ProblemJson);
}
