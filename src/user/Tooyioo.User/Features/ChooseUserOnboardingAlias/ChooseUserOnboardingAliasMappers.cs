using Microsoft.AspNetCore.Http;
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
                new UserOnboardingId(urlParams.UserOnboardingId.ToString()),
                request.Alias.Trim());
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<ChooseUserOnboardingAliasCommandResult, HttpContext, ChooseUserOnboardingAliasResponse>
    {
        public IResult Map(
            ChooseUserOnboardingAliasCommandResult commandResult,
            HttpContext context,
            CommandHttpResponseGenerator<ChooseUserOnboardingAliasResponse> commandHttpResponseGenerator)
            => commandResult switch
            {
                ChooseUserOnboardingAliasCommandOkResult ok => commandHttpResponseGenerator.Ok(
                    new ChooseUserOnboardingAliasResponse
                    {
                        UserId = ok.UserId.Value,
                        UserOnboardingId = ok.UserOnboardingId.Value
                    }),
                ChooseUserOnboardingAliasAlreadyClaimedError aliasAlreadyClaimedError =>
                    commandHttpResponseGenerator.UnprocessableEntity(
                        context,
                        "alias_already_claimed",
                        $"The provided user onboarding alias is already in use by user {aliasAlreadyClaimedError.UserId.Value}"),
                ChooseUserOnboardingAliasAlreadyChosenError aliasAlreadyChosenError =>
                    commandHttpResponseGenerator.UnprocessableEntity(
                        context,
                        "alias_already_chosen",
                        $"A different alias {aliasAlreadyChosenError.ExistingAlias} had already been chosen for this onboarding"),
                ChooseUserOnboardingAliasNotInProgressError => commandHttpResponseGenerator.UnprocessableEntity(
                    context,
                    "onboarding_not_in_progress",
                    "Could not choose user onboarding alias because onboarding is no longer in progress"),
                ChooseUserOnboardingAliasConcurrencyConflictError => commandHttpResponseGenerator.Conflict(
                    context,
                    "concurrent_update",
                    "Could not choose user onboarding alias because of a concurrency conflict, please retry"),
                ChooseUserOnboardingAliasUnexpectedStateError => commandHttpResponseGenerator.Conflict(
                    context,
                    "onboarding_state_unexpected",
                    "Could not choose user onboarding alias because of an unexpected state conflict, please retry")
            };
    }
}
