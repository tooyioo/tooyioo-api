global using InitiateBusinessOnboardingHttpResult =
    Microsoft.AspNetCore.Http.HttpResults.Results<
        Microsoft.AspNetCore.Http.HttpResults.Created<Tooyioo.Business.Features.InitiateBusinessOnboarding.Contracts.InitiateBusinessOnboardingResponse>,
        Microsoft.AspNetCore.Http.HttpResults.Conflict<Slicent.Application.HttpProblemDetails>>;

using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Commands;
using Tooyioo.Business.Features.InitiateBusinessOnboarding.Contracts;
using Tooyioo.Common;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global

namespace Tooyioo.Business.Features.InitiateBusinessOnboarding;

public static class InitiateBusinessOnboardingMappers
{
    public sealed class CommandMapper
        : ICommandMapper<InitiateBusinessOnboardingRequest, HttpContext, InitiateBusinessOnboardingCommand>
    {
        public InitiateBusinessOnboardingCommand Map(
            InitiateBusinessOnboardingRequest request,
            HttpContext context)
        {
            var userId = context.User.GetClaim(Claims.UserId)
                ?? throw new InvalidOperationException($"JWT is missing the '{Claims.UserId}' claim");

            return new InitiateBusinessOnboardingCommand(
                BusinessId.New(),
                request.LegalName.Trim(),
                new BusinessUserId(userId));
        }
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<InitiateBusinessOnboardingCommandResult, HttpContext, InitiateBusinessOnboardingHttpResult>
    {


        public InitiateBusinessOnboardingHttpResult Map(
            InitiateBusinessOnboardingCommandResult commandResult,
            HttpContext context)
            => commandResult switch
            {
                InitiateBusinessOnboardingOkResult ok => TypedResults.Created(
                    $"/businesses/{ok.BusinessId.Value}",
                    new InitiateBusinessOnboardingResponse
                    {
                        BusinessId = ok.BusinessId.Value
                    }),
                InitiateBusinessOnboardingConcurrencyError => TypedResults.Conflict(HttpProblemDetails.Conflict(
                    context,
                    "concurrent_update",
                    "Could not initiate business onboarding because of a concurrency conflict, please retry")),
                InitiateBusinessOnboardingUnexpectedStateError => TypedResults.Conflict(HttpProblemDetails.Conflict(
                    context,
                    "onboarding_state_unexpected",
                    "Could not initiate business onboarding because of an unexpected state conflict, please retry"))
            };
    }
}
