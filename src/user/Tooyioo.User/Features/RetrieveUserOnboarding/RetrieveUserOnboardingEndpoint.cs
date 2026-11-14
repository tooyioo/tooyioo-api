using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Authorization;
using Slicent.Application.Queries;
using Tooyioo.User.Features.RetrieveUserOnboarding.Contracts;

// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.RetrieveUserOnboarding;

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
                "/users/{id:guid}/onboarding",
                cfg => cfg
                    .WithName("retrieve-user-onboarding")
                    .WithSummary("Get a user onboarding")
                    .WithTags("users"))
            .RequireRouteAuthorization<RetrieveUserOnboardingRoute, RetrieveUserOnboardingAuthorizer>();
}
