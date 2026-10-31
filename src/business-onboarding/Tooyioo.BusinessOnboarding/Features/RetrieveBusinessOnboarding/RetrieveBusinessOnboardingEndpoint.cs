using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Queries;
using Tooyioo.BusinessOnboarding.Features.RetrieveBusinessOnboarding.Contracts;
using Tooyioo.Common;
// ReSharper disable UnusedType.Global

namespace Tooyioo.BusinessOnboarding.Features.RetrieveBusinessOnboarding;

public sealed class RetrieveBusinessOnboardingEndpoint
    : IHttpEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
        => app
            .MapQuery<
                RetrieveBusinessOnboardingRoute,
                RetrieveBusinessOnboardingQuery,
                RetrieveBusinessOnboardingQueryResult,
                RetrieveBusinessOnboardingResponse>(
                "/business-onboarding/{id:guid}",
                cfg => cfg
                    .WithName("retrieve-business-onboarding")
                    .WithSummary("Get a business onboarding")
                    .WithTags("business-onboarding"))
            .RequireAuthorization(AuthorizationPolicies.UserRequired)
            .WithQueryConventions<RetrieveBusinessOnboardingResponse, HttpProblemDetails>();
}
