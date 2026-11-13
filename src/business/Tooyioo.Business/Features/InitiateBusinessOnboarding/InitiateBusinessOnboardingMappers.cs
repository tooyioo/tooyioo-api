using Microsoft.AspNetCore.Http;
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
                BusinessOnboardingId.New(),
                request.LegalName.Trim(),
                new UserId(userId));
        }
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<InitiateBusinessOnboardingCommandResult, HttpContext,
            InitiateBusinessOnboardingResponse>
    {
        public IResult Map(
            InitiateBusinessOnboardingCommandResult commandResult,
            HttpContext context,
            CommandHttpResponseGenerator<InitiateBusinessOnboardingResponse> commandHttpResponseGenerator)
            => commandResult switch
            {
                InitiateBusinessOnboardingOkResult ok => commandHttpResponseGenerator.Ok(
                    new InitiateBusinessOnboardingResponse
                    {
                        BusinessOnboardingId = ok.BusinessOnboardingId
                    }),
                InitiateBusinessOnboardingConcurrencyError => commandHttpResponseGenerator.Conflict(
                    context,
                    "concurrent_update",
                    "Could not initiate business onboarding because of a concurrency conflict, please retry"),
                InitiateBusinessOnboardingUnexpectedStateError => commandHttpResponseGenerator.Conflict(
                    context,
                    "onboarding_state_unexpected",
                    "Could not initiate business onboarding because of an unexpected state conflict, please retry")
            };
    }
}
