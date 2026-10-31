using System.Security.Claims;
using Tooyioo.Common;

namespace Tooyioo.UserOnboarding.Features.InitiateUserOnboarding.Support;

internal static class PersonalDetailsClaimsParser
{
    public static PersonalDetails Parse(ClaimsPrincipal principal)
        => new(
            GetRequiredClaim(principal, Claims.GivenName),
            GetOptionalClaim(principal, Claims.FamilyName),
            GetRequiredClaim(principal, Claims.Email),
            GetRequiredBooleanClaim(principal, Claims.EmailVerified));

    private static string GetRequiredClaim(ClaimsPrincipal principal, string claim)
    {
        var claimOption = principal.GetClaim(claim);
        return claimOption.IsSome(out var value) 
            ? value 
            : throw new InvalidOperationException($"JWT is missing the '{claim}' claim");
    }
    
    private static string GetOptionalClaim(ClaimsPrincipal principal, string claim)
    {
        var claimOption = principal.GetClaim(claim);

        return claimOption.IsSome(out var value)
            ? value
            : string.Empty;
    }

    private static bool GetRequiredBooleanClaim(ClaimsPrincipal principal, string claim)
    {
        var value = GetRequiredClaim(principal, claim);
        return bool.TryParse(value, out var parsed) 
            ? parsed 
            : throw new InvalidOperationException($"JWT claim '{claim}' must be a boolean value");
    }
}

public sealed record PersonalDetails(string Name, string LastName, string Email, bool IsEmailVerified);
