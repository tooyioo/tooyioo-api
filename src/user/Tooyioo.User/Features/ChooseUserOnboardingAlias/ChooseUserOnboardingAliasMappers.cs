global using ChooseUserOnboardingAliasHttpResult =
    Microsoft.AspNetCore.Http.HttpResults.Results<
        Microsoft.AspNetCore.Http.HttpResults.Ok<Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts.ChooseUserOnboardingAliasResponse>,
        Microsoft.AspNetCore.Http.HttpResults.NotFound<Slicent.Application.HttpProblemDetails>,
        Microsoft.AspNetCore.Http.HttpResults.Conflict<Slicent.Application.HttpProblemDetails>,
        Microsoft.AspNetCore.Http.HttpResults.UnprocessableEntity<Slicent.Application.HttpProblemDetails>>;

using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Commands;
using Tooyioo.User.Features.ChooseUserOnboardingAlias.Contracts;
// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.ChooseUserOnboardingAlias;

public static class ChooseUserOnboardingAliasMappers
{
    public sealed class CommandMapper
        : ICommandMapper<ChooseUserOnboardingAliasRoute, ChooseUserOnboardingAliasRequest, HttpContext,
            ChooseUserOnboardingAliasCommand>
    {
        public ChooseUserOnboardingAliasCommand Map(
            ChooseUserOnboardingAliasRoute urlParams,
            ChooseUserOnboardingAliasRequest request,
            HttpContext context)
            => new(
                new UserId(urlParams.UserId.ToString()),
                request.Alias.Trim());
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<ChooseUserOnboardingAliasCommandResult, HttpContext, ChooseUserOnboardingAliasHttpResult>
    {
        public ChooseUserOnboardingAliasHttpResult Map(
            ChooseUserOnboardingAliasCommandResult commandResult,
            HttpContext context)
            => commandResult switch
            {
                ChooseUserOnboardingAliasCommandOkResult ok => TypedResults.Ok(
                    new ChooseUserOnboardingAliasResponse
                    {
                        UserId = ok.UserId.Value
                    }),
                ChooseUserOnboardingAliasAlreadyClaimedError aliasAlreadyClaimedError =>
                    TypedResults.UnprocessableEntity(HttpProblemDetails.UnprocessableEntity(
                        context,
                        "alias_already_claimed",
                        $"The provided user onboarding alias is already in use by user {aliasAlreadyClaimedError.UserId.Value}")),
                ChooseUserOnboardingAliasAlreadyChosenError aliasAlreadyChosenError =>
                    TypedResults.UnprocessableEntity(HttpProblemDetails.UnprocessableEntity(
                        context,
                        "alias_already_chosen",
                        $"A different alias {aliasAlreadyChosenError.ExistingAlias} had already been chosen for this onboarding")),
                ChooseUserOnboardingAliasNotInProgressError => TypedResults.UnprocessableEntity(HttpProblemDetails.UnprocessableEntity(
                    context,
                    "onboarding_not_in_progress",
                    "Could not choose user onboarding alias because onboarding is no longer in progress")),
                ChooseUserOnboardingAliasConcurrencyConflictError => TypedResults.Conflict(HttpProblemDetails.Conflict(
                    context,
                    "concurrent_update",
                    "Could not choose user onboarding alias because of a concurrency conflict, please retry")),
                ChooseUserOnboardingAliasNotFoundError => TypedResults.NotFound(HttpProblemDetails.NotFound(
                    context,
                    "user_onboarding_not_found",
                    "No user onboarding exists for the given user"))
            };
    }
}
