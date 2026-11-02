namespace Tooyioo.User.Features.RequestUserPersonalDataErasure.Contracts;

/// <summary>
/// Response returned after a successful personal data erasure request.
/// </summary>
/// <example>{"userId": "00000000-0000-0000-0000-000000000001", "status": "erasure_requested", "eraseAfterUtc": "2026-11-16T00:00:00.000Z"}</example>
public sealed record RequestUserPersonalDataErasureResponse
{
    /// <summary>
    /// The user whose personal data will be erased.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The current erasure status: <c>erasure_requested</c>.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// The instant after which the erasure is applied.
    /// </summary>
    public required DateTime EraseAfterUtc { get; init; }
}
