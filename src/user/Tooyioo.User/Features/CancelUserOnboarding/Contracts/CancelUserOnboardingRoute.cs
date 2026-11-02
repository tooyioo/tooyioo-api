using Microsoft.AspNetCore.Mvc;
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.CancelUserOnboarding.Contracts;

public record CancelUserOnboardingRoute
{
    [FromRoute(Name = "id")]
    public required Guid UserOnboardingId { get; init; }
}
