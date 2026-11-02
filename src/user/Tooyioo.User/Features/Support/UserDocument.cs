using Slicent.Application.Queries;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.Support;

public sealed record UserDocument
    : Document
{
    internal const string StatusInProgress = "in_progress";
    internal const string StatusCompleted = "completed";
    internal const string StatusCanceled = "canceled";
    internal const string StatusErased = "erased";

    public UserDocument(string Id)
        : base(Id)
    {
    }

    public required string UserOnboardingId { get; init; }
    public string? Alias { get; init; }
    public required string Name { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required bool IsEmailVerified { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Status { get; init; }
    public string? TermsOfServiceVersion { get; init; }
    public string? PrivacyPolicyVersion { get; init; }
}
