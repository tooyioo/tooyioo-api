using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Slicent.Application.Authorization;
using Tooyioo.Common;
using Tooyioo.User.Features.RetrieveUserPersonalData.Contracts;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUserPersonalData;

public sealed class RetrieveUserPersonalDataAuthorizer
    : IRouteAuthorizer<RetrieveUserPersonalDataRoute>
{
    public Task<bool> Authorize(
        ClaimsPrincipal claimsPrincipal,
        RetrieveUserPersonalDataRoute route,
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
