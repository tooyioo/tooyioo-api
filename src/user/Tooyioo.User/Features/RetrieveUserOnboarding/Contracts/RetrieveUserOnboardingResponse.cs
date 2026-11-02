// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Tooyioo.User.Features.RetrieveUserOnboarding.Contracts;

/// <summary>
/// Response to retrieve a user onboarding
/// </summary>
/// <example>
/// {"userId": "00000000-0000-0000-0000-000000000010", "userOnboardingId": "00000000-0000-0000-0000-000000000001",
/// "alias": "joe-bloggs-spain_123", "name": "Joe", "lastName": "Bloggs", "email": "joe@test.com",
/// "isEmailVerified": true, "status": "completed", "isComplete": true, "cancellationReasonCode": null}
/// </example>
public sealed record RetrieveUserOnboardingResponse
{
    internal const string StatusInProgress = "in_progress";
    internal const string StatusCompleted = "completed";
    internal const string StatusCanceled = "canceled";

    /// <summary>
    /// The person identifier
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The onboarding process identifier
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
    /// Cancellation reason code (if any)
    /// </summary>
    public required string? CancellationReasonCode { get; init; }
}
