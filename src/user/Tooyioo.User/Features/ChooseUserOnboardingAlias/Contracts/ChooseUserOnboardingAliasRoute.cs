using Microsoft.AspNetCore.Mvc;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;

public record ChooseUserOnboardingAliasRoute
{
    [FromRoute(Name = "id")] 
    public required Guid UserOnboardingId { get; init; }
}