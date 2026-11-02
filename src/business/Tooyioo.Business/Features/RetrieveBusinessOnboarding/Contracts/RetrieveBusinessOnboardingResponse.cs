// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Tooyioo.Business.Features.RetrieveBusinessOnboarding.Contracts;

/// <summary>
/// Response returned after a successful retrieve business onboarding request.
/// </summary>
/// <example>
/// {"businessOnboardingId": "00000000-0000-0000-0000-000000000100", "legalName": "Acme Ltd",
/// "initiatedBy": "00000000-0000-0000-0000-000000000010", "status": "in_progress",
/// "createdAt": "2026-01-01T00:00:00.000Z", "lastModifiedAt": "2026-01-01T00:00:00.000Z"}
/// </example>
public sealed record RetrieveBusinessOnboardingResponse
{
    internal const string StatusInProgress = "in_progress";

    /// <summary>
    /// The unique identifier of the business onboarding.
    /// </summary>
    public required string BusinessOnboardingId { get; init; }

    /// <summary>
    /// The legal name of the business being onboarded.
    /// </summary>
    public required string LegalName { get; init; }

    /// <summary>
    /// The app-owned user id that initiated the business onboarding.
    /// </summary>
    public required string InitiatedBy { get; init; }

    /// <summary>
    /// Current onboarding status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Creation date.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Last modification date.
    /// </summary>
    public required DateTime LastModifiedAt { get; init; }

}
