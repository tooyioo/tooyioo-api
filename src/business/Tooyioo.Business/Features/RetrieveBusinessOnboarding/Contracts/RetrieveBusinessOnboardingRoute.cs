using Microsoft.AspNetCore.Mvc;
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.Business.Features.RetrieveBusinessOnboarding.Contracts;

public record RetrieveBusinessOnboardingRoute
{
    [FromRoute(Name = "id")]
    public required Guid BusinessId { get; init; }
}
