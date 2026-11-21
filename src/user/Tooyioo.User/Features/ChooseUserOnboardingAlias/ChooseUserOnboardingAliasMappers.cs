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
        : ICommandHttpResponseMapper<ChooseUserOnboardingAliasCommandResult, HttpContext, ChooseUserOnboardingAliasResponse>
    {

        public HttpResponseMetadata Responses { get; } = new(
            SuccessStatusCodes: [StatusCodes.Status200OK],
            ErrorStatusCodes: [StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status422UnprocessableEntity]);

        public IResult Map(
            ChooseUserOnboardingAliasCommandResult commandResult,
            HttpContext context,
            CommandHttpResponseGenerator<ChooseUserOnboardingAliasResponse> commandHttpResponseGenerator)
            => commandResult switch
            {
                ChooseUserOnboardingAliasCommandOkResult ok => commandHttpResponseGenerator.Ok(
                    new ChooseUserOnboardingAliasResponse
                    {
                        UserId = ok.UserId.Value
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
                ChooseUserOnboardingAliasNotFoundError => commandHttpResponseGenerator.NotFound(
                    context,
                    "user_onboarding_not_found",
                    "No user onboarding exists for the given user")
            };
    }
}
