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
                InitiateUserOnboardingCommandResult, InitiateUserOnboardingHttpResult>(
                "/users",
                cfg
                    => cfg
                        .WithName("initiate-user-onboarding")
                        .WithSummary("Create a user and start its onboarding")
                        .WithDescription(
                            "Creates the user from the authenticated user login, records the personal data and starts onboarding. The server mints the user_id and returns 201 Created with its Location on the first request. A repeated request for an already claimed credential returns 200 OK with the existing user_id and appends nothing.")
                        .WithTags("users"))
            .RequireAuthorization(AuthorizationPolicies.SubRequired);
}
