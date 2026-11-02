namespace Tooyioo.User.Features.CancelUserPersonalDataErasure.Contracts;

/// <summary>
/// Response returned after a successful personal data erasure cancellation.
/// </summary>
/// <example>{"userId": "00000000-0000-0000-0000-000000000001", "status": "active"}</example>
public sealed record CancelUserPersonalDataErasureResponse
{
    /// <summary>
    /// The user whose erasure was canceled.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The current erasure status: <c>active</c>.
    /// </summary>
    public required string Status { get; init; }
}
