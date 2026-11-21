global using RequestUserPersonalDataErasureHttpResult =
    Microsoft.AspNetCore.Http.HttpResults.Results<
        Microsoft.AspNetCore.Http.HttpResults.Ok<Tooyioo.User.Features.RequestUserPersonalDataErasure.Contracts.RequestUserPersonalDataErasureResponse>,
        Microsoft.AspNetCore.Http.HttpResults.NotFound<Slicent.Application.HttpProblemDetails>,
        Microsoft.AspNetCore.Http.HttpResults.Conflict<Slicent.Application.HttpProblemDetails>,
        Microsoft.AspNetCore.Http.HttpResults.UnprocessableEntity<Slicent.Application.HttpProblemDetails>>;

using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Commands;
using Tooyioo.User.Features.RequestUserPersonalDataErasure.Contracts;

namespace Tooyioo.User.Features.RequestUserPersonalDataErasure;

public static class RequestUserPersonalDataErasureMappers
{
    public sealed class CommandMapper
        : ICommandMapper<RequestUserPersonalDataErasureRoute, RequestUserPersonalDataErasureRequest, HttpContext,
            RequestUserPersonalDataErasureCommand>
    {
        public RequestUserPersonalDataErasureCommand Map(
            RequestUserPersonalDataErasureRoute urlParams,
            RequestUserPersonalDataErasureRequest request,
            HttpContext context)
            => new(new UserId(urlParams.UserId.ToString()), request.ReasonCode.Trim());
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<RequestUserPersonalDataErasureCommandResult, HttpContext, RequestUserPersonalDataErasureHttpResult>
    {
        public RequestUserPersonalDataErasureHttpResult Map(
            RequestUserPersonalDataErasureCommandResult commandResult,
            HttpContext context)
            => commandResult switch
            {
                RequestUserPersonalDataErasureCommandOkResult ok => TypedResults.Ok(
                    new RequestUserPersonalDataErasureResponse
                    {
                        UserId = ok.UserId.Value,
                        Status = "erasure_requested",
                        EraseAfterUtc = ok.EraseAfterUtc
                    }),
                RequestUserPersonalDataErasureNotFoundError => TypedResults.NotFound(HttpProblemDetails.NotFound(
                    context,
                    "user_personal_data_not_found",
                    "Could not find personal data for the given user")),
                RequestUserPersonalDataErasureAlreadyErasedError => TypedResults.UnprocessableEntity(HttpProblemDetails.UnprocessableEntity(
                    context,
                    "user_personal_data_already_erased",
                    "The personal data has already been erased")),
                RequestUserPersonalDataErasureConcurrencyConflictError => TypedResults.Conflict(HttpProblemDetails.Conflict(
                    context,
                    "concurrent_update",
                    "Could not request personal data erasure because of a concurrency conflict, please retry"))
            };
    }
}
