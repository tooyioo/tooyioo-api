using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Commands;
using Tooyioo.Common;
using Tooyioo.UserOnboarding.Features.InitiateUserOnboarding.Contracts;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedType.Global

namespace Tooyioo.UserOnboarding.Features.InitiateUserOnboarding;

public sealed class InitiateUserOnboardingEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapCommand<InitiateUserOnboardingRequest, InitiateUserOnboardingCommand,
                InitiateUserOnboardingCommandResult,
                InitiateUserOnboardingResponse>(
                "user-onboarding/initiate",
                cfg
                    => cfg
                        .WithName("initiate-user-onboarding")
                        .WithSummary("Initiate user onboarding")
                        .WithDescription(
                            "Uses the authenticated external identity to start onboarding. Repeated requests for the same identity return its existing onboarding.")
                        .WithTags("user-onboarding"))
            .RequireAuthorization(AuthorizationPolicies.ExternalIdentityRequired)
            .WithCommandConventions<InitiateUserOnboardingResponse, HttpProblemDetails>();
}
