using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Slicent.Application;

/// <summary>
/// OpenAPI response conventions applied by the endpoint mapping helpers. The mapper owns the
/// endpoint-specific status codes; the helper only adds the errors produced before the mapper
/// runs (malformed request and authentication/authorization).
/// </summary>
internal static class OpenApiResponseConventions
{
    private const string Json = "application/json";
    private const string ProblemJson = "application/problem+json";

    public static RouteHandlerBuilder WithCommandResponses<TResponse>(
        this RouteHandlerBuilder builder,
        HttpResponseMetadata responses)
        where TResponse : class
        => WithResponses<TResponse>(builder, responses);

    public static RouteHandlerBuilder WithQueryResponses<TResponse>(
        this RouteHandlerBuilder builder,
        HttpResponseMetadata responses)
        where TResponse : class
        => WithResponses<TResponse>(builder, responses);

    private static RouteHandlerBuilder WithResponses<TResponse>(
        RouteHandlerBuilder builder,
        HttpResponseMetadata responses)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(responses);

        foreach (var successStatus in responses.SuccessStatusCodes)
        {
            builder = builder.Produces<TResponse>(successStatus, Json);
        }

        foreach (var errorStatus in responses.ErrorStatusCodes)
        {
            builder = builder.Produces<HttpProblemDetails>(errorStatus, ProblemJson);
        }

        return builder
            .Produces<HttpProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status403Forbidden, ProblemJson);
    }
}
