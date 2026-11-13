using System.Security.Claims;
// ReSharper disable ConvertToExtensionBlock

namespace Tooyioo.Common;

public static class ClaimsPrincipalExtensions
{
    public static string? GetClaim(this ClaimsPrincipal claimsPrincipal, string claim)
        => claimsPrincipal.FindFirstValue(claim);
}