using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Commands;
using Tooyioo.User.Features.CancelUserPersonalDataErasure.Contracts;

namespace Tooyioo.User.Features.CancelUserPersonalDataErasure;

public sealed class CancelUserPersonalDataErasureEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapDelete<
                CancelUserPersonalDataErasureRoute,
                CancelUserPersonalDataErasureCommand,
                CancelUserPersonalDataErasureCommandResult,
                CancelUserPersonalDataErasureResponse>(
                "/users/{id:guid}/personal-data/erasure",
                cfg => cfg
                    .WithName("cancel-user-personal-data-erasure")
                    .WithSummary("Remove the personal data erasure request")
                    .WithDescription(
                        "Removes the pending erasure request of the authenticated user. It is rejected once the purge has started applying it.")
                    .WithTags("user-personal-data"))
            .RequireRouteAuthorization<CancelUserPersonalDataErasureRoute, CancelUserPersonalDataErasureAuthorizer>();
}
