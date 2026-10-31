// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Tooyioo.BusinessOnboarding.Features.RetrieveBusinessOnboarding.Contracts;

/// <summary>
/// Response returned after a successful retrieve business onboarding request.
/// </summary>
/// <example>
/// {"businessOnboardingId": "00000000-0000-0000-0000-000000000100", "legalName": "Acme Ltd",
/// "initiatedBy": "00000000-0000-0000-0000-000000000010", "status": "in_progress",
/// "createdAt": "2026-01-01T00:00:00.000Z", "lastModifiedAt": "2026-01-01T00:00:00.000Z",
/// "completedAt": null, "canceledAt": null, "cancellationReason": null}
/// </example>
public sealed record RetrieveBusinessOnboardingResponse
{
    internal const string StatusInProgress = "in_progress";
    internal const string StatusCompleted = "completed";
    internal const string StatusCanceled = "canceled";

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
    /// Current onboarding status: <c>in_progress</c>, <c>completed</c> or <c>canceled</c>.
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

    /// <summary>
    /// Completion date, if completed.
    /// </summary>
    public required DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Cancellation date, if canceled.
    /// </summary>
    public required DateTime? CanceledAt { get; init; }

    /// <summary>
    /// Cancellation reason, if canceled.
    /// </summary>
    public required string? CancellationReason { get; init; }
}
