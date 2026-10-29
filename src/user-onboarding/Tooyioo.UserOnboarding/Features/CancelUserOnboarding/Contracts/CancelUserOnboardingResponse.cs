namespace Tooyioo.UserOnboarding.Features.CancelUserOnboarding.Contracts;

/// <summary>
/// Response returned after a successful user onboarding cancellation
/// </summary>
/// <remarks>
/// It contains the unique identifier of the user onboarding
/// </remarks>
/// <example>{"userOnboardingId": "00000000-0000-0000-0000-000000000001"}</example>
public sealed record CancelUserOnboardingResponse
{
    /// <summary>
    /// The unique identifier of the user onboarding
    /// </summary>
    public required string UserOnboardingId { get; init; }
}
