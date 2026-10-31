using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Commands;
using Tooyioo.BusinessOnboarding.Features.InitiateBusinessOnboarding.Contracts;
using Tooyioo.Common;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedType.Global

namespace Tooyioo.BusinessOnboarding.Features.InitiateBusinessOnboarding;

public sealed class InitiateBusinessOnboardingEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapCommand<InitiateBusinessOnboardingRequest, InitiateBusinessOnboardingCommand,
                InitiateBusinessOnboardingCommandResult,
                InitiateBusinessOnboardingResponse>(
                "business-onboarding/initiate",
                cfg
                    => cfg
                        .WithName("initiate-business-onboarding")
                        .WithSummary("Initiate business onboarding")
                        .WithDescription(
                            "Starts a business onboarding process for the authenticated user.")
                        .WithTags("business-onboarding"))
            .RequireAuthorization(AuthorizationPolicies.UserRequired)
            .WithCommandConventions<InitiateBusinessOnboardingResponse, HttpProblemDetails>();
}
