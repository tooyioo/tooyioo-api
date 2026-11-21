using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Commands;
using Tooyioo.User.Features.CompleteUserOnboarding.Contracts;

namespace Tooyioo.User.Features.CompleteUserOnboarding;

public sealed class CompleteUserOnboardingEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapCommand<
                CompleteUserOnboardingRoute,
                CompleteUserOnboardingRequest,
                CompleteUserOnboardingCommand,
                CompleteUserOnboardingCommandResult,
                CompleteUserOnboardingHttpResult>(
                "/users/{id:guid}/onboarding/completion",
                cfg => cfg
                    .WithName("complete-user-onboarding")
                    .WithSummary("Set the user onboarding completion")
                    .WithDescription(
                        "Sets the completion of an onboarding that already has a chosen alias and returns the resulting user. Repeating the same completion returns the existing user.")
                    .WithTags("users"),
                CommandHttpMethod.Put)
            .RequireRouteAuthorization<CompleteUserOnboardingRoute, CompleteUserOnboardingAuthorizer>();
}
