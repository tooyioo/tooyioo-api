namespace Tooyioo.UserOnboarding.Features.CompleteUserOnboarding.Contracts;

/// <summary>
/// Response returned after a successful complete user onboarding request
/// </summary>
/// <remarks>
/// It contains the unique identifier of the user onboarding
/// </remarks>
/// <example>{"userOnboardingId": "00000000-0000-0000-0000-000000000001", "userId": "00000000-0000-0000-0000-000000000002"}</example>
public sealed record CompleteUserOnboardingResponse
{
    /// <summary>
    /// The unique identifier of the user onboarding
    /// </summary>
    public required string UserOnboardingId { get; init; }
    
    /// <summary>
    /// The unique identifier of the user successfully onboarded
    /// </summary>
    public required string UserId { get; init; }
}
