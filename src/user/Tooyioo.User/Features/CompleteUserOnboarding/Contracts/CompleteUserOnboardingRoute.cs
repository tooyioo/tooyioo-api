using Microsoft.AspNetCore.Mvc;
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.CompleteUserOnboarding.Contracts;

public record CompleteUserOnboardingRoute
{
    [FromRoute(Name = "id")] 
    public required Guid UserOnboardingId { get; init; }
}