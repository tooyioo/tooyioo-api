using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Slicent.Application;

/// <summary>
/// Common error metadata for command and query endpoints. The success and domain-error statuses
/// come from the endpoint mapper's typed result.
/// </summary>
internal static class OpenApiResponseConventions
{
    private const string ProblemJson = "application/problem+json";

    public static RouteHandlerBuilder WithErrorResponses(this RouteHandlerBuilder builder)
        => builder
            .Produces<HttpProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)
            .Produces<HttpProblemDetails>(StatusCodes.Status403Forbidden, ProblemJson);
}
