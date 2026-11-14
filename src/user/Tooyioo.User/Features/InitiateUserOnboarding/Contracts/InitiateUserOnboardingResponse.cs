namespace Tooyioo.User.Features.InitiateUserOnboarding.Contracts;

/// <summary>
/// Response returned after a successful initiate user onboarding request
/// </summary>
/// <remarks>
/// It contains the unique identifier of the user being onboarded, which is also the identity of its onboarding process
/// </remarks>
/// <example>{"userId": "00000000-0000-0000-0000-000000000010"}</example>
public sealed record InitiateUserOnboardingResponse
{
    /// <summary>
    /// The unique identifier of the user being onboarded
    /// </summary>
    public required string UserId { get; init; }
}
