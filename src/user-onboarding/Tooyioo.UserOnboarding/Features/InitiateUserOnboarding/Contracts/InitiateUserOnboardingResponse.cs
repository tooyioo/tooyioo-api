namespace Tooyioo.UserOnboarding.Features.InitiateUserOnboarding.Contracts;

/// <summary>
/// Response returned after a successful initiate user onboarding request
/// </summary>
/// <remarks>
/// It contains the unique identifier of the user onboarding
/// </remarks>
/// <example>{"userOnboardingId": "00000000-0000-0000-0000-000000000001"}</example>
public sealed record InitiateUserOnboardingResponse
{
    /// <summary>
    /// The unique identifier of the user onboarding
    /// </summary>
    public required string UserOnboardingId { get; init; }
}
