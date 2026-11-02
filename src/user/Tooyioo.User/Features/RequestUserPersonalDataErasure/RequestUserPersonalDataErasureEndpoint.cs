using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Commands;
using Tooyioo.Common;
using Tooyioo.User.Features.RequestUserPersonalDataErasure.Contracts;

namespace Tooyioo.User.Features.RequestUserPersonalDataErasure;

public sealed class RequestUserPersonalDataErasureEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapCommand<
                RequestUserPersonalDataErasureRoute,
                RequestUserPersonalDataErasureRequest,
                RequestUserPersonalDataErasureCommand,
                RequestUserPersonalDataErasureCommandResult,
                RequestUserPersonalDataErasureResponse>(
                "/users/{id:guid}/personal-data/erase",
                cfg => cfg
                    .WithName("request-user-personal-data-erasure")
                    .WithSummary("Request personal data erasure")
                    .WithDescription(
                        "Starts the right-to-be-forgotten grace period for the authenticated user. The data remains readable until the grace period ends, and the request can be canceled meanwhile.")
                    .WithTags("user-personal-data"))
            .RequireRouteAuthorization<RequestUserPersonalDataErasureRoute, RequestUserPersonalDataErasureAuthorizer>()
            .WithCommandConventions<RequestUserPersonalDataErasureResponse, HttpProblemDetails>();
}
