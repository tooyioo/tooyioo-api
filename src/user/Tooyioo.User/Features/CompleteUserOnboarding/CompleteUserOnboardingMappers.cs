using Microsoft.AspNetCore.Http;
using Slicent.Application.Commands;
using Tooyioo.User.Features.CompleteUserOnboarding.Contracts;

namespace Tooyioo.User.Features.CompleteUserOnboarding;

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
            => new(
                new UserOnboardingId(urlParams.UserOnboardingId.ToString()),
                request.TermsOfServiceVersion,
                request.PrivacyPolicyVersion);
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
                        UserId = ok.UserId.Value,
                        UserOnboardingId = ok.UserOnboardingId.Value
                    }),
                err => err.Match<IResult>(
                    _ => commandHttpResponseGenerator.Conflict(
                        context,
                        "onboarding_already_canceled",
                        "Could not complete user onboarding because it has been canceled"),
                    _ => commandHttpResponseGenerator.Conflict(
                        context,
                        "onboarding_alias_not_chosen",
                        "Could not complete user onboarding because no alias has been chosen"),
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
