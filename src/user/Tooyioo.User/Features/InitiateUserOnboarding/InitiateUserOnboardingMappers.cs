using Microsoft.AspNetCore.Http;
using Slicent.Application;
using Slicent.Application.Commands;
using Tooyioo.User.Identity;
using Tooyioo.User.Features.InitiateUserOnboarding.Contracts;
using Tooyioo.User.Features.InitiateUserOnboarding.Support;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global

namespace Tooyioo.User.Features.InitiateUserOnboarding;

public static class InitiateUserOnboardingMappers
{
    public sealed class CommandMapper
        : ICommandMapper<InitiateUserOnboardingRequest, HttpContext, InitiateUserOnboardingCommand>
    {
        private readonly IExternalIdentityRetriever _externalIdentityRetriever;
        private readonly IUserLoginKeyFactory _userLoginKeyFactory;

        public CommandMapper(
            IExternalIdentityRetriever externalIdentityRetriever,
            IUserLoginKeyFactory userLoginKeyFactory)
        {
            _externalIdentityRetriever = externalIdentityRetriever;
            _userLoginKeyFactory = userLoginKeyFactory;
        }

        public InitiateUserOnboardingCommand Map(InitiateUserOnboardingRequest request, HttpContext context)
        {
            var externalIdentity = _externalIdentityRetriever.GetExternalIdentityFromContext(context);
            var personalDetails = PersonalDetailsClaimsParser.Parse(context.User);
            var userLoginKey = _userLoginKeyFactory.ForOidc(externalIdentity.Issuer, externalIdentity.Id);

            return new InitiateUserOnboardingCommand(
                UserId.New(),
                userLoginKey,
                personalDetails.Name.Trim(),
                personalDetails.LastName.Trim(),
                personalDetails.Email.Trim(),
                personalDetails.IsEmailVerified);
        }
    }

    public sealed class CommandHttpResponseMapper
        : ICommandHttpResponseMapper<InitiateUserOnboardingCommandResult, HttpContext, InitiateUserOnboardingResponse>
    {

        public HttpResponseMetadata Responses { get; } = new(
            SuccessStatusCodes: [StatusCodes.Status201Created, StatusCodes.Status200OK],
            ErrorStatusCodes: [StatusCodes.Status409Conflict]);

        public IResult Map(
            InitiateUserOnboardingCommandResult commandResult,
            HttpContext context,
            CommandHttpResponseGenerator<InitiateUserOnboardingResponse> commandHttpResponseGenerator)
            => commandResult switch
            {
                InitiateUserOnboardingCreatedResult created => commandHttpResponseGenerator.Created(
                    $"/users/{created.UserId.Value}",
                    new InitiateUserOnboardingResponse
                    {
                        UserId = created.UserId.Value
                    }),
                InitiateUserOnboardingExistingResult existing => commandHttpResponseGenerator.Ok(
                    new InitiateUserOnboardingResponse
                    {
                        UserId = existing.UserId.Value
                    }),
                InitiateUserOnboardingConcurrencyError => commandHttpResponseGenerator.Conflict(
                    context,
                    "concurrent_update",
                    "Could not initiate user onboarding because of a concurrency conflict, please retry"),
                InitiateUserOnboardingUnexpectedStateError => commandHttpResponseGenerator.Conflict(
                    context,
                    "onboarding_state_unexpected",
                    "Could not initiate user onboarding because of an unexpected state conflict, please retry")
            };
    }
}
