using Microsoft.AspNetCore.Mvc;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUserOnboarding.Contracts;

public record RetrieveUserOnboardingRoute
{
    [FromRoute(Name = "id")]
    public required Guid UserId { get; init; }
}
