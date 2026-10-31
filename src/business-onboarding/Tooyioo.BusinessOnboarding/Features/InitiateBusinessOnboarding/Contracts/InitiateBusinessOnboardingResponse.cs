namespace Tooyioo.BusinessOnboarding.Features.InitiateBusinessOnboarding.Contracts;

/// <summary>
/// Response returned after a successful initiate business onboarding request
/// </summary>
/// <remarks>
/// It contains the unique identifier of the business onboarding
/// </remarks>
/// <example>{"businessOnboardingId": "00000000-0000-0000-0000-000000000001"}</example>
public sealed record InitiateBusinessOnboardingResponse
{
    /// <summary>
    /// The unique identifier of the business onboarding
    /// </summary>
    public required string BusinessOnboardingId { get; init; }
}
