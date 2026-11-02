namespace Tooyioo.User.Features.InitiateUserOnboarding.Contracts;

/// <summary>
/// Response returned after a successful initiate user onboarding request
/// </summary>
/// <remarks>
/// It contains the unique identifier of the user being onboarded and its onboarding identifier
/// </remarks>
/// <example>{"userId": "00000000-0000-0000-0000-000000000010",
/// "userOnboardingId": "00000000-0000-0000-0000-000000000001"}</example>
public sealed record InitiateUserOnboardingResponse
{
    /// <summary>
    /// The unique identifier of the user being onboarded
    /// </summary>
    public required string UserId { get; init; }
    
    /// <summary>
    /// The unique identifier of the user onboarding process
    /// </summary>
    public required string UserOnboardingId { get; init; }
}
