using Microsoft.AspNetCore.Mvc;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.CancelUserPersonalDataErasure.Contracts;

public record CancelUserPersonalDataErasureRoute
{
    [FromRoute(Name = "id")]
    public required Guid UserId { get; init; }
}
