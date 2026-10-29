namespace Tooyioo.UserOnboarding.Features.ChooseUserOnboardingAlias.Contracts;

/// <summary>
/// Response returned after successfully choosing user onboarding alias
/// </summary>
/// <example>{"userOnboardingId": "00000000-0000-0000-0000-000000000001"}</example>
public sealed record ChooseUserOnboardingAliasResponse
{
    /// <summary>
    /// The unique identifier of the user onboarding
    /// </summary>
    public required string UserOnboardingId { get; init; }
}
