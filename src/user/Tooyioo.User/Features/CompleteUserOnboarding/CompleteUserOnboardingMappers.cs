using Microsoft.AspNetCore.Http;
using Slicent.Application;
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
                new UserId(urlParams.UserId.ToString()),
                request.TermsOfServiceVersion,
                request.PrivacyPolicyVersion);
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<CompleteUserOnboardingCommandResult, HttpContext, CompleteUserOnboardingResponse>
    {

        public HttpResponseMetadata Responses { get; } = new(
            SuccessStatusCodes: [StatusCodes.Status200OK],
            ErrorStatusCodes: [StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status422UnprocessableEntity]);

        public IResult Map(
            CompleteUserOnboardingCommandResult commandResult,
            HttpContext context,
            CommandHttpResponseGenerator<CompleteUserOnboardingResponse> commandHttpResponseGenerator)
            => commandResult switch
            {
                CompleteUserOnboardingCommandOkResult ok => commandHttpResponseGenerator.Ok(
                    new CompleteUserOnboardingResponse
                    {
                        UserId = ok.UserId.Value
                    }),
                CompleteUserOnboardingAlreadyCanceledError => commandHttpResponseGenerator.UnprocessableEntity(
                    context,
                    "onboarding_already_canceled",
                    "Could not complete user onboarding because it has been canceled"),
                CompleteUserOnboardingAliasNotChosenError => commandHttpResponseGenerator.UnprocessableEntity(
                    context,
                    "onboarding_alias_not_chosen",
                    "Could not complete user onboarding because no alias has been chosen"),
                CompleteUserOnboardingConcurrencyConflict => commandHttpResponseGenerator.Conflict(
                    context,
                    "concurrent_update",
                    "Could not complete user onboarding because of a concurrency conflict, please retry"),
                CompleteUserOnboardingNotFoundError => commandHttpResponseGenerator.NotFound(
                    context,
                    "user_onboarding_not_found",
                    "No user onboarding exists for the given user")
            };
    }
}
