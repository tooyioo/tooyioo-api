using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Slicent.Application.Authorization;
using Tooyioo.Common;
using Tooyioo.User.Features.RetrieveUser.Contract;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUser;

public sealed class RetrieveUserAuthorizer
    : IRouteAuthorizer<RetrieveUserRoute>
{
    public Task<bool> Authorize(
        ClaimsPrincipal claimsPrincipal, 
        RetrieveUserRoute route, 
        HttpContext http, 
        CancellationToken ct)
    {
        if (claimsPrincipal.GetClaim(Claims.UserId) is not { } userId)
        {
            return Task.FromResult(false);
        }
      
        // TODO some users could retrieve other users' data
        var isSameProfile = route.UserId.ToString() == userId;
        return Task.FromResult(isSameProfile);
    }
}