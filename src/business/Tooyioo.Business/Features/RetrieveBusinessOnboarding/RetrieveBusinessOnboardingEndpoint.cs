using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Slicent.Application;
using Slicent.Application.Queries;
using Tooyioo.Business.Features.RetrieveBusinessOnboarding.Contracts;
using Tooyioo.Common;
// ReSharper disable UnusedType.Global

namespace Tooyioo.Business.Features.RetrieveBusinessOnboarding;

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
                "/businesses/{id:guid}/onboarding",
                cfg => cfg
                    .WithName("retrieve-business-onboarding")
                    .WithSummary("Get the business onboarding")
                    .WithTags("business"))
            .RequireAuthorization(AuthorizationPolicies.UserRequired);
}
