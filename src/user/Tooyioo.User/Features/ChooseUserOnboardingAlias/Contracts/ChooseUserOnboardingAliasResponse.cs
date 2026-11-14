namespace Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;

/// <summary>
/// Response returned after successfully choosing user onboarding alias
/// </summary>
/// <example>{"userId": "00000000-0000-0000-0000-000000000010"}</example>
public sealed record ChooseUserOnboardingAliasResponse
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    public required string UserId { get; init; }
}
