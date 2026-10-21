using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Slicent.Application.Authorization;
using Tooyioo.Common;
using Tooyioo.UserOnboarding.Features.RetrieveUserOnboarding.Contracts;
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.UserOnboarding.Features.RetrieveUserOnboarding;

public sealed class RetrieveUserOnboardingAuthorizer
    : IRouteAuthorizer<RetrieveUserOnboardingRoute>
{
    public Task<bool> Authorize(
        ClaimsPrincipal claimsPrincipal,
        RetrieveUserOnboardingRoute route,
        HttpContext http,
        CancellationToken ct)
    {
        var userOnboardingIdOption = claimsPrincipal.GetClaim(Claims.UserOnboardingId);
        if (!userOnboardingIdOption.IsSome(out var userOnboardingId))
        {
            return Task.FromResult(false);
        }

        var isExpectedUserOnboardingId = route.UserOnboardingId.ToString() == userOnboardingId;
        return Task.FromResult(isExpectedUserOnboardingId);
    }
}
