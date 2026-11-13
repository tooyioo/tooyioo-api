using Microsoft.AspNetCore.Http;
using Slicent.Application.Commands;
using Tooyioo.User.Features.RequestUserPersonalDataErasure.Contracts;
using Tooyioo.Common;

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
        : ICommandHttpResponseMapper<RequestUserPersonalDataErasureCommandResult, HttpContext, RequestUserPersonalDataErasureResponse>
    {
        public IResult Map(
            RequestUserPersonalDataErasureCommandResult commandResult,
            HttpContext context,
            CommandHttpResponseGenerator<RequestUserPersonalDataErasureResponse> commandHttpResponseGenerator)
            => commandResult switch
            {
                RequestUserPersonalDataErasureCommandOkResult ok => commandHttpResponseGenerator.Ok(
                    new RequestUserPersonalDataErasureResponse
                    {
                        UserId = ok.UserId.Value,
                        Status = "erasure_requested",
                        EraseAfterUtc = ok.EraseAfterUtc
                    }),
                RequestUserPersonalDataErasureNotFoundError => commandHttpResponseGenerator.NotFound(
                    context,
                    "user_personal_data_not_found",
                    "Could not find personal data for the given user"),
                RequestUserPersonalDataErasureAlreadyErasedError => commandHttpResponseGenerator.UnprocessableEntity(
                    context,
                    "user_personal_data_already_erased",
                    "The personal data has already been erased"),
                RequestUserPersonalDataErasureConcurrencyConflictError => commandHttpResponseGenerator.Conflict(
                    context,
                    "concurrent_update",
                    "Could not request personal data erasure because of a concurrency conflict, please retry")
            };
    }
}
