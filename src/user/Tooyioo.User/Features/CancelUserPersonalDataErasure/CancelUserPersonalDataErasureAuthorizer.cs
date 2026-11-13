using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Slicent.Application.Authorization;
using Tooyioo.Common;
using Tooyioo.User.Features.CancelUserPersonalDataErasure.Contracts;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.CancelUserPersonalDataErasure;

public sealed class CancelUserPersonalDataErasureAuthorizer
    : IRouteAuthorizer<CancelUserPersonalDataErasureRoute>
{
    public Task<bool> Authorize(
        ClaimsPrincipal claimsPrincipal,
        CancelUserPersonalDataErasureRoute route,
        HttpContext http,
        CancellationToken ct)
    {
        if (claimsPrincipal.GetClaim(Claims.UserId) is not { } userId)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(route.UserId.ToString() == userId);
    }
}
