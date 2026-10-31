using Slicent.Application.Queries;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.BusinessOnboarding.Features.Support;

public sealed record BusinessOnboardingDocument
    : Document
{
    internal const string StatusInProgress = "in_progress";
    internal const string StatusCompleted = "completed";
    internal const string StatusCanceled = "canceled";

    public BusinessOnboardingDocument(string Id)
        : base(Id)
    {
    }

    public required string LegalName { get; init; }
    public required string InitiatedBy { get; init; }
    public required string Status { get; init; }
    public required DateTime? CompletedAt { get; init; }
    public required DateTime? CanceledAt { get; init; }
    public required string? CancellationReason { get; init; }
}
