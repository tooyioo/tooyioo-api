using Microsoft.AspNetCore.Http;
using Slicent.Application.Commands;
using Tooyioo.BusinessOnboarding.Features.InitiateBusinessOnboarding.Contracts;
using Tooyioo.Common;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global

namespace Tooyioo.BusinessOnboarding.Features.InitiateBusinessOnboarding;

public static class InitiateBusinessOnboardingMappers
{
    public sealed class CommandMapper
        : ICommandMapper<InitiateBusinessOnboardingRequest, HttpContext, InitiateBusinessOnboardingCommand>
    {
        public InitiateBusinessOnboardingCommand Map(
            InitiateBusinessOnboardingRequest request,
            HttpContext context)
        {
            var userIdOption = context.User.GetClaim(Claims.UserId);
            if (!userIdOption.IsSome(out var userId))
            {
                throw new InvalidOperationException($"JWT is missing the '{Claims.UserId}' claim");
            }

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
            => commandResult.Match<IResult>(
                ok => commandHttpResponseGenerator.Ok(new InitiateBusinessOnboardingResponse
                {
                    BusinessOnboardingId = ok.BusinessOnboardingId
                }),
                err => err.Match<IResult>(
                    _ => commandHttpResponseGenerator.Conflict(
                        context,
                        "concurrent_update",
                        "Could not initiate business onboarding because of a concurrency conflict, please retry"),
                    _ => commandHttpResponseGenerator.Conflict(
                        context,
                        "onboarding_state_unexpected",
                        "Could not initiate business onboarding because of an unexpected state conflict, please retry")
                ));
    }
}
