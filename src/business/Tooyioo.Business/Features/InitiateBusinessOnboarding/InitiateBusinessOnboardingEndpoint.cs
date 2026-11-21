using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Commands;
using Tooyioo.Business.Features.InitiateBusinessOnboarding.Contracts;
using Tooyioo.Common;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedType.Global

namespace Tooyioo.Business.Features.InitiateBusinessOnboarding;

public sealed class InitiateBusinessOnboardingEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapCommand<InitiateBusinessOnboardingRequest, InitiateBusinessOnboardingCommand,
                InitiateBusinessOnboardingCommandResult,
                InitiateBusinessOnboardingHttpResult>(
                "/businesses",
                cfg
                    => cfg
                        .WithName("initiate-business-onboarding")
                        .WithSummary("Create a business and start its onboarding")
                        .WithDescription(
                            "Creates the business for the authenticated user and starts its onboarding. The server mints the business_id and returns it in the Location header.")
                        .WithTags("business"))
            .RequireAuthorization(AuthorizationPolicies.UserRequired);
}
