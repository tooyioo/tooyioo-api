using Eventuous;
using Funzo;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.Common;
using Tooyioo.UserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.InitiateUserOnboarding.Support;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.UserOnboarding.Features.InitiateUserOnboarding;

public sealed class InitiateUserOnboardingHandler
    : ICommandHandler<InitiateUserOnboardingCommand, InitiateUserOnboardingCommandResult>
{
    private readonly IEventReader _eventReader;
    private readonly IEventWriter _eventWriter;

    public InitiateUserOnboardingHandler(
        IEventReader eventReader,
        IEventWriter eventWriter)
    {
        _eventReader = eventReader;
        _eventWriter = eventWriter;
    }

    public async Task<InitiateUserOnboardingCommandResult> Handle(
        InitiateUserOnboardingCommand command, 
        CancellationToken cancellationToken = default)
    {
        var externalIdentity = command.ExternalIdentity;

        var claimingExternalIdentity =
            await _eventReader.LoadStateOrNew<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
                externalIdentity.Id,
                cancellationToken);

        if (claimingExternalIdentity.State.UserOnboardingId is { } existingUserOnboardingId)
        {
            // Idempotent: trusted ExternalId means this is the same principal retrying.
            return new InitiateUserOnboardingOkResult(existingUserOnboardingId);
        }
        
        var userOnboardingId = UserOnboardingId.New();
        var userOnboarding =
            await _eventReader.LoadStateOrNew<UserOnboardingState, UserOnboardingId>(userOnboardingId, cancellationToken);
        
        if (userOnboarding.Events.Length != 0)
        {
            return new InitiateUserOnboardingUnexpectedStateError();
        }

        var userOnboardingInitiated = new UserOnboardingDomainEvents.V1.UserOnboardingInitiated(
            command.Name,
            command.LastName,
            command.Email,
            command.IsEmailVerified,
            externalIdentity.Id,
            externalIdentity.Provider.ToString(),
            externalIdentity.Issuer);

        var externalIdentityClaimed =
            new ExternalIdentityClaimingDomainEvents.V1.ExternalIdentityClaimed(userOnboardingId);

        try
        {
            _ = await _eventWriter.StoreStateChanges(
                [
                    userOnboarding.ToStateStreamChanges([userOnboardingInitiated]),
                    claimingExternalIdentity.ToStateStreamChanges([externalIdentityClaimed])
                ],
                cancellationToken);

            return new InitiateUserOnboardingOkResult(userOnboardingId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new InitiateUserOnboardingConcurrencyError();
        }
    }
}

public sealed record InitiateUserOnboardingCommand(
    string Name, 
    string LastName, 
    string Email, 
    bool IsEmailVerified,
    ExternalIdentity ExternalIdentity)
    : ICommand<InitiateUserOnboardingCommandResult>;

[Result<InitiateUserOnboardingOkResult, InitiateUserOnboardingCommandErrorResult>]
public partial class InitiateUserOnboardingCommandResult;

public sealed record InitiateUserOnboardingOkResult(UserOnboardingId UserOnboardingId);

[Union<InitiateUserOnboardingConcurrencyError, InitiateUserOnboardingUnexpectedStateError>]
public partial class InitiateUserOnboardingCommandErrorResult;

public sealed record InitiateUserOnboardingConcurrencyError;
public sealed record InitiateUserOnboardingUnexpectedStateError;
