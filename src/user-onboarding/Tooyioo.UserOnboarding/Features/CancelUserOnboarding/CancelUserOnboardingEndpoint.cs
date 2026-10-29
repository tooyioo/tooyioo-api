using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Commands;
using Tooyioo.Common;
using Tooyioo.UserOnboarding.Features.CancelUserOnboarding.Contracts;

namespace Tooyioo.UserOnboarding.Features.CancelUserOnboarding;

public sealed class CancelUserOnboardingEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapCommand<
                CancelUserOnboardingRoute,
                CancelUserOnboardingRequest,
                CancelUserOnboardingCommand,
                CancelUserOnboardingCommandResult,
                CancelUserOnboardingResponse>(
                "/user-onboarding/{id:guid}/cancel",
                cfg => cfg
                    .WithName("cancel-user-onboarding")
                    .WithSummary("Cancel user onboarding")
                    .WithDescription(
                        "Cancels an in-progress onboarding and releases its external identity and chosen alias. Repeating the request for a canceled onboarding succeeds without changing state.")
                    .WithTags("user-onboarding"))
            .RequireRouteAuthorization<CancelUserOnboardingRoute, CancelUserOnboardingAuthorizer>()
            .WithCommandConventions<CancelUserOnboardingResponse, HttpProblemDetails>();
}
