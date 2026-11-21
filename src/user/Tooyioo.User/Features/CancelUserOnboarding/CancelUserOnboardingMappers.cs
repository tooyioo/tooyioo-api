global using CancelUserOnboardingHttpResult =
    Microsoft.AspNetCore.Http.HttpResults.Results<
        Microsoft.AspNetCore.Http.HttpResults.Ok<Tooyioo.User.Features.CancelUserOnboarding.Contracts.CancelUserOnboardingResponse>,
        Microsoft.AspNetCore.Http.HttpResults.NotFound<Slicent.Application.HttpProblemDetails>,
        Microsoft.AspNetCore.Http.HttpResults.Conflict<Slicent.Application.HttpProblemDetails>,
        Microsoft.AspNetCore.Http.HttpResults.UnprocessableEntity<Slicent.Application.HttpProblemDetails>>;

using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Commands;
using Tooyioo.User.Features.CancelUserOnboarding.Contracts;

namespace Tooyioo.User.Features.CancelUserOnboarding;

public static class CancelUserOnboardingMappers
{
    public sealed class CommandMapper
        : ICommandMapper<CancelUserOnboardingRoute, CancelUserOnboardingRequest, HttpContext,
            CancelUserOnboardingCommand>
    {
        public CancelUserOnboardingCommand Map(
            CancelUserOnboardingRoute urlParams,
            CancelUserOnboardingRequest request,
            HttpContext context)
            => new(new UserId(urlParams.UserId.ToString()), request.ReasonCode.Trim());
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<CancelUserOnboardingCommandResult, HttpContext, CancelUserOnboardingHttpResult>
    {
        public CancelUserOnboardingHttpResult Map(
            CancelUserOnboardingCommandResult commandResult,
            HttpContext context)
            => commandResult switch
            {
                CancelUserOnboardingCommandOkResult ok => TypedResults.Ok(
                    new CancelUserOnboardingResponse
                    {
                        UserId = ok.UserId.Value
                    }),
                CancelUserOnboardingAlreadyCompletedError => TypedResults.UnprocessableEntity(HttpProblemDetails.UnprocessableEntity(
                    context,
                    "onboarding_already_completed",
                    "Could not cancel user onboarding because it has already been completed")),
                CancelUserOnboardingConcurrencyConflictError => TypedResults.Conflict(HttpProblemDetails.Conflict(
                    context,
                    "concurrent_update",
                    "Could not cancel user onboarding because of a concurrency conflict, please retry")),
                CancelUserOnboardingNotFoundError => TypedResults.NotFound(HttpProblemDetails.NotFound(
                    context,
                    "user_onboarding_not_found",
                    "No user onboarding exists for the given user")),
                CancelUserOnboardingUnexpectedStateError => TypedResults.Conflict(HttpProblemDetails.Conflict(
                    context,
                    "onboarding_state_unexpected",
                    "Could not cancel user onboarding because of an unexpected state conflict, please retry"))
            };
    }
}
