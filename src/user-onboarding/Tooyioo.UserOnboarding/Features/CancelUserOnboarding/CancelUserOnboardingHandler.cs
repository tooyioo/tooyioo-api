using Eventuous;
using Funzo;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.UserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.Support;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.UserOnboarding.Features.CancelUserOnboarding;

public sealed class CancelUserOnboardingHandler
    : ICommandHandler<CancelUserOnboardingCommand, CancelUserOnboardingCommandResult>
{
    private readonly IEventReader _eventReader;
    private readonly IEventWriter _eventWriter;

    public CancelUserOnboardingHandler(
        IEventReader eventReader,
        IEventWriter eventWriter)
    {
        _eventReader = eventReader;
        _eventWriter = eventWriter;
    }

    public async Task<CancelUserOnboardingCommandResult> Handle(
        CancelUserOnboardingCommand command,
        CancellationToken cancellationToken = default)
    {
        var userOnboardingId = command.UserOnboardingId;

        var userOnboarding =
            await _eventReader.LoadStateOrNew<UserOnboardingState, UserOnboardingId>(userOnboardingId, cancellationToken);

        if (userOnboarding.Events.Length == 0)
        {
            return new CancelUserOnboardingUnexpectedStateError();
        }

        if (userOnboarding.State.IsComplete)
        {
            return new CancelUserOnboardingAlreadyCompletedError();
        }

        if (userOnboarding.State.IsCanceled)
        {
            return new CancelUserOnboardingCommandOkResult(userOnboardingId);
        }

        var externalIdentityClaiming =
            await _eventReader.LoadStateOrNew<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
                userOnboarding.State.ExternalIdentityId,
                cancellationToken);

        if (externalIdentityClaiming.State.UserOnboardingId != userOnboardingId)
        {
            return new CancelUserOnboardingUnexpectedStateError();
        }

        var stateChanges = new List<StateStreamChanges>
        {
            userOnboarding.ToStateStreamChanges([
                new UserOnboardingDomainEvents.V1.UserOnboardingCanceled(command.Reason)
            ]),
            externalIdentityClaiming.ToStateStreamChanges([
                new ExternalIdentityClaimingDomainEvents.V1.ExternalIdentityReleased(userOnboardingId)
            ])
        };

        if (userOnboarding.State.Alias is { } alias)
        {
            var aliasClaiming =
                await _eventReader.LoadStateOrNew<ClaimingUserAliasState, ClaimingUserAliasId>(
                    new ClaimingUserAliasId(alias),
                    cancellationToken);

            if (aliasClaiming.State.UserOnboardingId != userOnboardingId)
            {
                return new CancelUserOnboardingUnexpectedStateError();
            }

            stateChanges.Add(aliasClaiming.ToStateStreamChanges([
                new UserAliasClaimingDomainEvents.V1.UserAliasReleased(userOnboardingId)
            ]));
        }

        try
        {
            _ = await _eventWriter.StoreStateChanges(stateChanges, cancellationToken);

            return new CancelUserOnboardingCommandOkResult(userOnboardingId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new CancelUserOnboardingConcurrencyConflictError();
        }
    }
}

public sealed record CancelUserOnboardingCommand(UserOnboardingId UserOnboardingId, string Reason)
    : ICommand<CancelUserOnboardingCommandResult>;

[Result<CancelUserOnboardingCommandOkResult, CancelUserOnboardingCommandErrorResult>]
public partial class CancelUserOnboardingCommandResult;

public sealed record CancelUserOnboardingCommandOkResult(UserOnboardingId UserOnboardingId);

[Union<CancelUserOnboardingAlreadyCompletedError, CancelUserOnboardingConcurrencyConflictError, CancelUserOnboardingUnexpectedStateError>]
public partial class CancelUserOnboardingCommandErrorResult;
public sealed record CancelUserOnboardingAlreadyCompletedError;
public sealed record CancelUserOnboardingConcurrencyConflictError;
public sealed record CancelUserOnboardingUnexpectedStateError;
