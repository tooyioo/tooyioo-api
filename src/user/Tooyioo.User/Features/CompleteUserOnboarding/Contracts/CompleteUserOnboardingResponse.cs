namespace Tooyioo.User.Features.CompleteUserOnboarding.Contracts;

/// <summary>
/// Response returned after a successful complete user onboarding request
/// </summary>
/// <example>{"userId": "00000000-0000-0000-0000-000000000010", "userOnboardingId": "00000000-0000-0000-0000-000000000001"}</example>
public sealed record CompleteUserOnboardingResponse
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The unique identifier of the user onboarding
    /// </summary>
    public required string UserOnboardingId { get; init; }
}
