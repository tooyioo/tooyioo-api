using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Slicent.Application.Authorization;
using Tooyioo.Common;

namespace Tooyioo.User.Features.Support;

public abstract class UserOnboardingRouteAuthorizer<TRoute>
    : IRouteAuthorizer<TRoute>
{
    public Task<bool> Authorize(
        ClaimsPrincipal claimsPrincipal,
        TRoute route,
        HttpContext http,
        CancellationToken cancellationToken = default)
    {
        var userId = claimsPrincipal.GetClaim(Claims.UserId);
        var authorized = userId is not null && GetUserId(route).ToString() == userId;

        return Task.FromResult(authorized);
    }

    protected abstract Guid GetUserId(TRoute route);
}
