// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Tooyioo.User.Features.RetrieveUser.Contract;

/// <summary>
/// Response returned after a successful retrieve user request.
/// </summary>
/// <example>
/// {"id": "00000000-0000-0000-0000-000000000002", "alias": "joe-bloggs-spain_123",
/// "name": "Joe", "lastName": "Bloggs", "email": "joe@test.com", "isEmailVerified": true,
/// "phoneNumber": "+34644000000", "status": "completed",
/// "createdAt": "2026-01-01T12:05:00.000Z", "lastModifiedAt": "2026-01-01T12:05:00.000Z",
/// "termsOfServiceVersion": "1.0.0", "privacyPolicyVersion": "1.0.0"}
/// </example>
public sealed record RetrieveUserResponse
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The public user alias.
    /// </summary>
    public required string Alias { get; init; }

    /// <summary>
    /// The user's given name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The user's family name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Whether the user's email address has been verified.
    /// </summary>
    public required bool IsEmailVerified { get; init; }

    /// <summary>
    /// The user's phone number, or an empty value when none is registered.
    /// </summary>
    public required string PhoneNumber { get; init; }

    /// <summary>
    /// The user's lifecycle status: <c>in_progress</c>, <c>completed</c>, <c>canceled</c> or <c>erased</c>.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// The UTC date and time when the user document was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// The UTC date and time when the user document was last modified.
    /// </summary>
    public required DateTime LastModifiedAt { get; init; }

    /// <summary>
    /// The terms of service version acknowledged by the user.
    /// </summary>
    public required string TermsOfServiceVersion { get; init; }

    /// <summary>
    /// The privacy policy version acknowledged by the user.
    /// </summary>
    public required string PrivacyPolicyVersion { get; init; }
}
