using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Queries;
using Tooyioo.Common;
using Tooyioo.UserOnboarding.Features.RetrieveUserOnboarding.Contracts;
// ReSharper disable UnusedType.Global

namespace Tooyioo.UserOnboarding.Features.RetrieveUserOnboarding;

public sealed class RetrieveUserOnboardingEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapQuery<
                RetrieveUserOnboardingRoute,
                RetrieveUserOnboardingQuery,
                RetrieveUserOnboardingQueryResult,
                RetrieveUserOnboardingResponse>(
                "/user-onboarding/{id:guid}",
                cfg => cfg
                    .WithName("retrieve-user-onboarding")
                    .WithSummary("Get a user onboarding")
                    .WithTags("user-onboarding"))
            .RequireRouteAuthorization<RetrieveUserOnboardingRoute, RetrieveUserOnboardingAuthorizer>()
            .WithQueryConventions<RetrieveUserOnboardingResponse, HttpProblemDetails>();
}
