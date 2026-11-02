using Slicent.Application.Queries;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.Business.Features.Support;

public sealed record BusinessOnboardingDocument
    : Document
{
    internal const string StatusInProgress = "in_progress";

    public BusinessOnboardingDocument(string Id)
        : base(Id)
    {
    }

    public required string LegalName { get; init; }
    public required string InitiatedBy { get; init; }
    public required string Status { get; init; }
}
