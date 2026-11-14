namespace Tooyioo.User.Features.CancelUserOnboarding.Contracts;

/// <summary>
/// Response returned after a successful user onboarding cancellation
/// </summary>
/// <example>{"userId": "00000000-0000-0000-0000-000000000010"}</example>
public sealed record CancelUserOnboardingResponse
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    public required string UserId { get; init; }
}
