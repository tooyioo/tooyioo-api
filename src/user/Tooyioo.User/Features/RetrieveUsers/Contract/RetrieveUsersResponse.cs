using Slicent.Application.Queries;
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUsers.Contract;

/// <summary>
/// Response returned after a successful retrieve users request.
/// </summary>
/// <example>
/// {"pageNumber": 1, "pageSize": 20, "totalPages": 1, "totalCount": 1,
/// "items": [{"id": "00000000-0000-0000-0000-000000000001", "alias": "joe-bloggs-spain_123",
/// "name": "Joe", "lastName": "Bloggs", "email": "joe@test.com", "isEmailVerified": true,
/// "phoneNumber": "+34644000000", "externalId": "google-sub-123", "externalProvider": "Google",
/// "userOnboardingId": "00000000-0000-0000-0000-000000000010",
/// "createdAt": "2026-01-01T12:05:00.000Z", "lastModifiedAt": "2026-01-01T12:05:00.000Z",
/// "termsAndConditionsVersion": "v1"}]}
/// </example>
public sealed record RetrieveUsersResponse
    : PagedResponse<RetrieveUsersResponseItem>;

public sealed record RetrieveUsersResponseItem
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
    /// The identifier assigned by the external identity provider.
    /// </summary>
    public required string ExternalId { get; init; }
    
    /// <summary>
    /// The external identity provider name.
    /// </summary>
    public required string ExternalProvider { get; init; }
    
    /// <summary>
    /// The user onboarding process identifier that originated the user.
    /// </summary>
    public required string UserOnboardingId { get; init; }
    
    /// <summary>
    /// The UTC date and time when the user document was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// The UTC date and time when the user document was last modified.
    /// </summary>
    public required DateTime LastModifiedAt { get; init; }
    
    /// <summary>
    /// The terms and conditions version acknowledged by the user.
    /// </summary>
    public required string TermsAndConditionsVersion { get; init; }
}
