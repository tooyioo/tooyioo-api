using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Commands;
using Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;

// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.ChooseUserOnboardingAlias;

public sealed class ChooseUserOnboardingAliasEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapCommand<
                ChooseUserOnboardingAliasRoute,
                ChooseUserOnboardingAliasRequest,
                ChooseUserOnboardingAliasCommand,
                ChooseUserOnboardingAliasCommandResult,
                ChooseUserOnboardingAliasHttpResult>(
                "/users/{id:guid}/onboarding/alias",
                cfg => cfg
                    .WithName("choose-user-onboarding-alias")
                    .WithSummary("Set the user onboarding alias")
                    .WithDescription(
                        "Sets the alias of an in-progress onboarding. Repeating the same alias is idempotent; a different alias cannot replace it.")
                    .WithTags("users"),
                CommandHttpMethod.Put)
            .RequireRouteAuthorization<ChooseUserOnboardingAliasRoute, ChooseUserOnboardingAliasAuthorizer>();
}
