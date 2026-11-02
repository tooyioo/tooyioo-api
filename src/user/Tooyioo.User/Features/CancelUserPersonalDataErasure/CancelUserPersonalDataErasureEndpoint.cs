using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Commands;
using Tooyioo.Common;
using Tooyioo.User.Features.CancelUserPersonalDataErasure.Contracts;

namespace Tooyioo.User.Features.CancelUserPersonalDataErasure;

public sealed class CancelUserPersonalDataErasureEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapCommand<
                CancelUserPersonalDataErasureRoute,
                CancelUserPersonalDataErasureRequest,
                CancelUserPersonalDataErasureCommand,
                CancelUserPersonalDataErasureCommandResult,
                CancelUserPersonalDataErasureResponse>(
                "/users/{id:guid}/personal-data/cancel-erasure",
                cfg => cfg
                    .WithName("cancel-user-personal-data-erasure")
                    .WithSummary("Cancel personal data erasure")
                    .WithDescription(
                        "Cancels a pending erasure during its grace period. It is rejected once the erasure has been applied.")
                    .WithTags("user-personal-data"))
            .RequireRouteAuthorization<CancelUserPersonalDataErasureRoute, CancelUserPersonalDataErasureAuthorizer>()
            .WithCommandConventions<CancelUserPersonalDataErasureResponse, HttpProblemDetails>();
}
