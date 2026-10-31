using Microsoft.AspNetCore.Mvc;
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.BusinessOnboarding.Features.RetrieveBusinessOnboarding.Contracts;

public record RetrieveBusinessOnboardingRoute
{
    [FromRoute(Name = "id")]
    public required Guid BusinessOnboardingId { get; init; }
}
