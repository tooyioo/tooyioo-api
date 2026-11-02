using System.Security.Claims;
using Tooyioo.Common;

namespace Tooyioo.Api.Infrastructure.Auth;

internal static class ExternalIdentityClaimsParser
{
    /// <summary>
    /// Reads the raw external identity claims. Issuer validation is the JWT middleware's
    /// responsibility; mapping the issuer to a provider happens in the user login key factory.
    /// </summary>
    public static ExternalIdentity Parser(ClaimsPrincipal claimsPrincipal)
    {
        if (!claimsPrincipal.GetClaim(Claims.Issuer).IsSome(out var issuer))
        {
            throw new InvalidOperationException($"JWT is missing the '{Claims.Issuer}' claim");
        }

        if (!claimsPrincipal.GetClaim(Claims.Sub).IsSome(out var subject))
        {
            throw new InvalidOperationException($"JWT is missing the '{Claims.Sub}' claim");
        }

        return new ExternalIdentity(subject, issuer);
    }
}
