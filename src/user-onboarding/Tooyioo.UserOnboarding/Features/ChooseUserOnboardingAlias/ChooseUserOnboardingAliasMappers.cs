using Microsoft.AspNetCore.Http;
using Slicent.Application.Commands;
using Tooyioo.UserOnboarding.Features.ChooseUserOnboardingAlias.Contracts;
// ReSharper disable UnusedType.Global

namespace Tooyioo.UserOnboarding.Features.ChooseUserOnboardingAlias;

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
            => commandResult.Match<IResult>(
                ok => commandHttpResponseGenerator.Ok(new ChooseUserOnboardingAliasResponse { UserOnboardingId = ok.UserOnboardingId }),
                err => err.Match<IResult>(
                    aliasAlreadyClaimedError => commandHttpResponseGenerator.Conflict(
                        context,
                        "alias_already_claimed",
                        $"The provided user onboarding alias is already in use by onboarding with Id {aliasAlreadyClaimedError.UserOnboardingId}"),
                    aliasAlreadyChosenError => commandHttpResponseGenerator.Conflict(
                        context,
                        "alias_already_chosen",
                        $"A different alias {aliasAlreadyChosenError.ExistingAlias} had already been chosen for this onboarding"),
                    _ => commandHttpResponseGenerator.Conflict(
                        context,
                        "onboarding_not_in_progress",
                        "Could not choose user onboarding alias because onboarding is no longer in progress"),
                    _ => commandHttpResponseGenerator.Conflict(
                        context,
                        "concurrent_update",
                        "Could not choose user onboarding alias because of a concurrency conflict, please retry"),
                    _ => commandHttpResponseGenerator.Conflict(
                        context,
                        "onboarding_state_unexpected",
                        "Could not choose user onboarding alias because of an unexpected state conflict, please retry")
                ));
    }
}
