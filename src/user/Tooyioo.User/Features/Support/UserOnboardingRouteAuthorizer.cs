using System.Security.Claims;
using Eventuous;
using Microsoft.AspNetCore.Http;
using Slicent.Application.Authorization;
using Slicent.EventStore;
using Tooyioo.Common;

namespace Tooyioo.User.Features.Support;

public abstract class UserOnboardingRouteAuthorizer<TRoute>(IEventReader eventReader)
    : IRouteAuthorizer<TRoute>
{
    public async Task<bool> Authorize(
        ClaimsPrincipal claimsPrincipal,
        TRoute route,
        HttpContext http,
        CancellationToken cancellationToken = default)
    {
        if (claimsPrincipal.GetClaim(Claims.UserId) is not { } userId)
        {
            return false;
        }

        var userOnboardingId = GetUserOnboardingId(route).ToString();
        var onboarding = 
            await eventReader.LoadStateOrNew<UserOnboardingState, UserOnboardingId>(
                new UserOnboardingId(userOnboardingId), cancellationToken);

        return onboarding.Events.Length == 0 || onboarding.State.UserId == userId;
    }

    protected abstract Guid GetUserOnboardingId(TRoute route);
}
