using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Commands;
using Tooyioo.Common;
using Tooyioo.User.Features.InitiateUserOnboarding.Contracts;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.InitiateUserOnboarding;

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
                            "Resolves the authenticated user login, records the personal data, and starts onboarding. Repeated requests for an already claimed credential return its existing user_id.")
                        .WithTags("user-onboarding"))
            .RequireAuthorization(AuthorizationPolicies.SubRequired)
            .WithCommandConventions<InitiateUserOnboardingResponse, HttpProblemDetails>();
}
