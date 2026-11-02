using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Slicent.Application.Authorization;
using Tooyioo.Common;
using Tooyioo.User.Features.RequestUserPersonalDataErasure.Contracts;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RequestUserPersonalDataErasure;

public sealed class RequestUserPersonalDataErasureAuthorizer
    : IRouteAuthorizer<RequestUserPersonalDataErasureRoute>
{
    public Task<bool> Authorize(
        ClaimsPrincipal claimsPrincipal,
        RequestUserPersonalDataErasureRoute route,
        HttpContext http,
        CancellationToken ct)
    {
        var userIdOption = claimsPrincipal.GetClaim(Claims.UserId);
        if (!userIdOption.IsSome(out var userId))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(route.UserId.ToString() == userId);
    }
}
