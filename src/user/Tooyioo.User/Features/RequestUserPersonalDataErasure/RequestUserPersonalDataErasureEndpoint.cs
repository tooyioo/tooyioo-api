using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Commands;
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
                RequestUserPersonalDataErasureHttpResult>(
                "/users/{id:guid}/personal-data/erasure",
                cfg => cfg
                    .WithName("request-user-personal-data-erasure")
                    .WithSummary("Set the personal data erasure request")
                    .WithDescription(
                        "Sets the erasure request of the authenticated user. Nothing is erased until an operator runs the purge; the request can be removed until then.")
                    .WithTags("user-personal-data"),
                CommandHttpMethod.Put)
            .RequireRouteAuthorization<RequestUserPersonalDataErasureRoute, RequestUserPersonalDataErasureAuthorizer>();
}
