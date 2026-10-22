using Slicent.Application.Queries;

// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.UserOnboarding.Features.RetrieveUserOnboarding.Support;

public sealed record UserOnboardingDocument
    : Document
{
    internal const string StatusInProgress = "in_progress";
    internal const string StatusCompleted = "completed";
    internal const string StatusCanceled = "canceled";
    
    public UserOnboardingDocument(string Id) 
        : base(Id)
    {
    }

    public required string Name { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required bool IsEmailVerified { get; init; }
    public string? Alias { get; init; }
    public required string ExternalIdentityId { get; init; }
    public required string ExternalIdentityProvider { get; init; }
    public required string ExternalIdentityIssuer { get; init; }
    public required string Status { get; init; }
    public required DateTime? CompletedAt { get; init; }
    public required DateTime? CanceledAt { get; init; }
    public required string? CancellationReason { get; init; }
}