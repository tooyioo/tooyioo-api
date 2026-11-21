global using CompleteUserOnboardingHttpResult =
    Microsoft.AspNetCore.Http.HttpResults.Results<
        Microsoft.AspNetCore.Http.HttpResults.Ok<Tooyioo.User.Features.CompleteUserOnboarding.Contracts.CompleteUserOnboardingResponse>,
        Microsoft.AspNetCore.Http.HttpResults.NotFound<Slicent.Application.HttpProblemDetails>,
        Microsoft.AspNetCore.Http.HttpResults.Conflict<Slicent.Application.HttpProblemDetails>,
        Microsoft.AspNetCore.Http.HttpResults.UnprocessableEntity<Slicent.Application.HttpProblemDetails>>;

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
        : ICommandHttpResponseMapper<CompleteUserOnboardingCommandResult, HttpContext, CompleteUserOnboardingHttpResult>
    {
        public CompleteUserOnboardingHttpResult Map(
            CompleteUserOnboardingCommandResult commandResult,
            HttpContext context)
            => commandResult switch
            {
                CompleteUserOnboardingCommandOkResult ok => TypedResults.Ok(
                    new CompleteUserOnboardingResponse
                    {
                        UserId = ok.UserId.Value
                    }),
                CompleteUserOnboardingAlreadyCanceledError => TypedResults.UnprocessableEntity(HttpProblemDetails.UnprocessableEntity(
                    context,
                    "onboarding_already_canceled",
                    "Could not complete user onboarding because it has been canceled")),
                CompleteUserOnboardingAliasNotChosenError => TypedResults.UnprocessableEntity(HttpProblemDetails.UnprocessableEntity(
                    context,
                    "onboarding_alias_not_chosen",
                    "Could not complete user onboarding because no alias has been chosen")),
                CompleteUserOnboardingConcurrencyConflict => TypedResults.Conflict(HttpProblemDetails.Conflict(
                    context,
                    "concurrent_update",
                    "Could not complete user onboarding because of a concurrency conflict, please retry")),
                CompleteUserOnboardingNotFoundError => TypedResults.NotFound(HttpProblemDetails.NotFound(
                    context,
                    "user_onboarding_not_found",
                    "No user onboarding exists for the given user"))
            };
    }
}
