using Microsoft.AspNetCore.Mvc;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RequestUserPersonalDataErasure.Contracts;

public record RequestUserPersonalDataErasureRoute
{
    [FromRoute(Name = "id")]
    public required Guid UserId { get; init; }
}
