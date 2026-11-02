using Microsoft.AspNetCore.Mvc;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.RetrieveUserPersonalData.Contracts;

public record RetrieveUserPersonalDataRoute
{
    [FromRoute(Name = "id")]
    public required Guid UserId { get; init; }
}
