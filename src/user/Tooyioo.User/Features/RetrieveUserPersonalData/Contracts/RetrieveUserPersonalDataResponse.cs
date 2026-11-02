// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Tooyioo.User.Features.RetrieveUserPersonalData.Contracts;

/// <summary>
/// Response returned after a successful retrieve personal data request.
/// </summary>
/// <example>
/// {"userId": "00000000-0000-0000-0000-000000000001", "name": "Joe", "lastName": "Bloggs",
/// "email": "joe@test.com", "isEmailVerified": true, "status": "active",
/// "eraseAfterUtc": null, "createdAt": "2026-01-01T00:00:00.000Z",
/// "lastModifiedAt": "2026-01-01T00:00:00.000Z"}
/// </example>
public sealed record RetrieveUserPersonalDataResponse
{
    /// <summary>
    /// The user whose personal data is returned.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The last name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// The email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Whether the email address is verified.
    /// </summary>
    public required bool IsEmailVerified { get; init; }

    /// <summary>
    /// The erasure status: <c>active</c>, <c>erasure_requested</c> or <c>erased</c>.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// The instant after which the erasure is applied, when one is pending.
    /// </summary>
    public required DateTime? EraseAfterUtc { get; init; }

    /// <summary>
    /// Creation date.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Last modification date.
    /// </summary>
    public required DateTime LastModifiedAt { get; init; }
}
