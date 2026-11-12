using Microsoft.AspNetCore.Http;
using Slicent.Application.Commands;
using Tooyioo.User.Features.CancelUserPersonalDataErasure.Contracts;
using Tooyioo.Common;

namespace Tooyioo.User.Features.CancelUserPersonalDataErasure;

public static class CancelUserPersonalDataErasureMappers
{
    public sealed class CommandMapper
        : ICommandMapper<CancelUserPersonalDataErasureRoute, CancelUserPersonalDataErasureRequest, HttpContext,
            CancelUserPersonalDataErasureCommand>
    {
        public CancelUserPersonalDataErasureCommand Map(
            CancelUserPersonalDataErasureRoute urlParams,
            CancelUserPersonalDataErasureRequest request,
            HttpContext context)
            => new(new UserId(urlParams.UserId.ToString()));
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<CancelUserPersonalDataErasureCommandResult, HttpContext, CancelUserPersonalDataErasureResponse>
    {
        public IResult Map(
            CancelUserPersonalDataErasureCommandResult commandResult,
            HttpContext context,
            CommandHttpResponseGenerator<CancelUserPersonalDataErasureResponse> commandHttpResponseGenerator)
            => commandResult.Match<IResult>(
                ok => commandHttpResponseGenerator.Ok(new CancelUserPersonalDataErasureResponse
                {
                    UserId = ok.UserId.Value,
                    Status = "active"
                }),
                err => err.Match<IResult>(
                    _ => commandHttpResponseGenerator.NotFound(
                        context,
                        "user_personal_data_not_found",
                        "Could not find personal data for the given user"),
                    _ => commandHttpResponseGenerator.UnprocessableEntity(
                        context,
                        "user_personal_data_already_erased",
                        "The personal data has already been erased"),
                    _ => commandHttpResponseGenerator.Conflict(
                        context,
                        "concurrent_update",
                        "Could not cancel personal data erasure because of a concurrency conflict, please retry")
                ));
    }
}
