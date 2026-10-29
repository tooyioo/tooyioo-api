using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Commands;
using Tooyioo.Common;
using Tooyioo.UserOnboarding.Features.ChooseUserOnboardingAlias.Contracts;

// ReSharper disable UnusedType.Global

namespace Tooyioo.UserOnboarding.Features.ChooseUserOnboardingAlias;

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
                ChooseUserOnboardingAliasResponse>(
                "/user-onboarding/{id:guid}/choose-alias",
                cfg => cfg
                    .WithName("choose-user-onboarding-alias")
                    .WithSummary("Choose an onboarding alias")
                    .WithDescription(
                        "Claims an available alias for an in-progress onboarding. Repeating the same choice is idempotent; a different alias cannot replace it.")
                    .WithTags("user-onboarding"))
            .RequireRouteAuthorization<ChooseUserOnboardingAliasRoute, ChooseUserOnboardingAliasAuthorizer>()
            .WithCommandConventions<ChooseUserOnboardingAliasResponse, HttpProblemDetails>();
}
