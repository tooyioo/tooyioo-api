global using CancelUserPersonalDataErasureHttpResult =
    Microsoft.AspNetCore.Http.HttpResults.Results<
        Microsoft.AspNetCore.Http.HttpResults.Ok<Tooyioo.User.Features.CancelUserPersonalDataErasure.Contracts.CancelUserPersonalDataErasureResponse>,
        Microsoft.AspNetCore.Http.HttpResults.NotFound<Slicent.Application.HttpProblemDetails>,
        Microsoft.AspNetCore.Http.HttpResults.Conflict<Slicent.Application.HttpProblemDetails>,
        Microsoft.AspNetCore.Http.HttpResults.UnprocessableEntity<Slicent.Application.HttpProblemDetails>>;

using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Commands;
using Tooyioo.User.Features.CancelUserPersonalDataErasure.Contracts;

namespace Tooyioo.User.Features.CancelUserPersonalDataErasure;

public static class CancelUserPersonalDataErasureMappers
{
    public sealed class CommandMapper
        : ICommandMapper<CancelUserPersonalDataErasureRoute, HttpContext, CancelUserPersonalDataErasureCommand>
    {
        public CancelUserPersonalDataErasureCommand Map(
            CancelUserPersonalDataErasureRoute urlParams,
            HttpContext context)
            => new(new UserId(urlParams.UserId.ToString()));
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<CancelUserPersonalDataErasureCommandResult, HttpContext, CancelUserPersonalDataErasureHttpResult>
    {
        public CancelUserPersonalDataErasureHttpResult Map(
            CancelUserPersonalDataErasureCommandResult commandResult,
            HttpContext context)
            => commandResult switch
            {
                CancelUserPersonalDataErasureCommandOkResult ok => TypedResults.Ok(
                    new CancelUserPersonalDataErasureResponse
                    {
                        UserId = ok.UserId.Value,
                        Status = "active"
                    }),
                CancelUserPersonalDataErasureNotFoundError => TypedResults.NotFound(HttpProblemDetails.NotFound(
                    context,
                    "user_personal_data_not_found",
                    "Could not find personal data for the given user")),
                CancelUserPersonalDataErasureAlreadyErasedError => TypedResults.UnprocessableEntity(HttpProblemDetails.UnprocessableEntity(
                    context,
                    "user_personal_data_already_erased",
                    "The personal data has already been erased")),
                CancelUserPersonalDataErasureConcurrencyConflictError => TypedResults.Conflict(HttpProblemDetails.Conflict(
                    context,
                    "concurrent_update",
                    "Could not cancel personal data erasure because of a concurrency conflict, please retry"))
            };
    }
}
