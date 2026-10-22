// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Tooyioo.UserOnboarding.Features.RetrieveUserOnboarding.Contracts;

/// <summary>
/// Response to retrieve a user onboarding
/// </summary>
/// <example>
/// {"id": "00000000-0000-0000-0000-000000000001", "alias": "joe-bloggs-spain_123", "email": "joe@test.com",
/// "phoneNumber": "+34644000000", "isComplete": true, "createdAt": "2026-01-01T00:00:00.123Z",
/// "lastModifiedAt": "2026-01-01T00:00:00.123Z", "completedAt": "2026-01-01T00:00:00.123Z"}
/// </example>
public sealed record RetrieveUserOnboardingResponse
{
    internal const string StatusInProgress = "in_progress";
    internal const string StatusCompleted = "completed";
    internal const string StatusCanceled = "canceled";
    
    /// <summary>
    /// The unique identifier of the user onboarding
    /// </summary>
    public required string UserOnboardingId { get; init; }

    /// <summary>
    /// The unique alias (if any)
    /// </summary>
    public required string? Alias { get; init; }

    /// <summary>
    /// The name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The last name
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// The email address
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Flag indicating whether the user email is verified
    /// </summary>
    public required bool IsEmailVerified { get; init; }

    /// <summary>
    /// Current onboarding status: <c>in_progress</c>, <c>completed</c> or <c>canceled</c>.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Flag indicating whether the user onboarding is complete or needs to be completed
    /// </summary>
    public bool IsComplete => Status == StatusCompleted;

    /// <summary>
    /// Creation date
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Last modification date
    /// </summary>
    public required DateTime LastModifiedAt { get; init; }

    /// <summary>
    /// Profile completion date (if any)
    /// </summary>
    public required DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Cancellation date (if any)
    /// </summary>
    public required DateTime? CanceledAt { get; init; }

    /// <summary>
    /// Cancellation reason (if any)
    /// </summary>
    public required string? CancellationReason { get; init; }
}
