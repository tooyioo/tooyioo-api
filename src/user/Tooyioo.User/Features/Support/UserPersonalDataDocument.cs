using Slicent.Application.Queries;

// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.Support;

public sealed record UserPersonalDataDocument
    : Document
{
    internal const string StatusActive = "active";
    internal const string StatusErasureRequested = "erasure_requested";
    internal const string StatusErased = "erased";

    public UserPersonalDataDocument(string Id)
        : base(Id)
    {
    }

    public required string Name { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required bool IsEmailVerified { get; init; }
    public required string Status { get; init; }
    public required bool IsErasureRequested { get; init; }
    public required bool IsErased { get; init; }
    public required DateTime? EraseAfterUtc { get; init; }
}
