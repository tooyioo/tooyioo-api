using Microsoft.AspNetCore.Http;
using Slicent.Application.Commands;
using Tooyioo.UserOnboarding.Features.CompleteUserOnboarding.Contracts;

namespace Tooyioo.UserOnboarding.Features.CompleteUserOnboarding;

public static class CompleteUserOnboardingMappers
{
    public sealed class CommandMapper
        : ICommandMapper<CompleteUserOnboardingRoute, CompleteUserOnboardingRequest, HttpContext, 
            CompleteUserOnboardingCommand>
    {
        public CompleteUserOnboardingCommand Map(
            CompleteUserOnboardingRoute urlParams,
            CompleteUserOnboardingRequest request, 
            HttpContext context)
            => new(new UserOnboardingId(urlParams.UserOnboardingId.ToString()), request.TermsAndConditionsVersion);
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<CompleteUserOnboardingCommandResult, HttpContext, CompleteUserOnboardingResponse>
    {
        public IResult Map(
            CompleteUserOnboardingCommandResult commandResult,
            HttpContext context,
            CommandHttpResponseGenerator<CompleteUserOnboardingResponse> commandHttpResponseGenerator)
            => commandResult.Match<IResult>(
                ok => commandHttpResponseGenerator.Ok(
                    new CompleteUserOnboardingResponse
                    {
                        UserOnboardingId = ok.UserOnboardingId, 
                        UserId = ok.UserId
                    }),
                err => err.Match<IResult>(
                    _ => commandHttpResponseGenerator.Conflict(
                        context,
                        "onboarding_already_canceled",
                        "Could not complete user onboarding because it has been canceled"),
                    _ => commandHttpResponseGenerator.Conflict(
                        context,
                        "concurrent_update",
                        "Could not complete user onboarding because of a concurrency conflict, please retry"),
                    _ => commandHttpResponseGenerator.Conflict(
                        context,
                        "onboarding_state_unexpected",
                        "Could not complete user onboarding because of an unexpected state conflict, please retry")

                ));
    }
}
