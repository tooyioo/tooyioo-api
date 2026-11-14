using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Commands;
using Tooyioo.User.Features.CancelUserOnboarding.Contracts;

namespace Tooyioo.User.Features.CancelUserOnboarding;

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
                "/users/{id:guid}/onboarding/cancellation",
                cfg => cfg
                    .WithName("cancel-user-onboarding")
                    .WithSummary("Set the user onboarding cancellation")
                    .WithDescription(
                        "Cancels an in-progress onboarding, releases its user login and chosen alias, and requests an immediate personal data erasure. Once the credential is released the request can no longer be authenticated.")
                    .WithTags("users"),
                CommandHttpMethod.Put)
            .RequireRouteAuthorization<CancelUserOnboardingRoute, CancelUserOnboardingAuthorizer>();
}
