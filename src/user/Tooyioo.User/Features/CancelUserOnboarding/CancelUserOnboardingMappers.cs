using Microsoft.AspNetCore.Http;
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
            => new(new UserOnboardingId(urlParams.UserOnboardingId.ToString()), request.ReasonCode.Trim());
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<CancelUserOnboardingCommandResult, HttpContext, CancelUserOnboardingResponse>
    {
        public IResult Map(
            CancelUserOnboardingCommandResult commandResult,
            HttpContext context,
            CommandHttpResponseGenerator<CancelUserOnboardingResponse> commandHttpResponseGenerator)
            => commandResult switch
            {
                CancelUserOnboardingCommandOkResult ok => commandHttpResponseGenerator.Ok(
                    new CancelUserOnboardingResponse
                    {
                        UserId = ok.UserId.Value,
                        UserOnboardingId = ok.UserOnboardingId.Value
                    }),
                CancelUserOnboardingAlreadyCompletedError => commandHttpResponseGenerator.UnprocessableEntity(
                    context,
                    "onboarding_already_completed",
                    "Could not cancel user onboarding because it has already been completed"),
                CancelUserOnboardingConcurrencyConflictError => commandHttpResponseGenerator.Conflict(
                    context,
                    "concurrent_update",
                    "Could not cancel user onboarding because of a concurrency conflict, please retry"),
                CancelUserOnboardingUnexpectedStateError => commandHttpResponseGenerator.Conflict(
                    context,
                    "onboarding_state_unexpected",
                    "Could not cancel user onboarding because of an unexpected state conflict, please retry")
            };
    }
}
