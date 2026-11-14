using Eventuous;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.User.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.User.Features.CancelUserOnboarding;

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
        var userId = command.UserId;

        var userOnboarding =
            await _eventReader.LoadStateOrNew<UserOnboardingState, UserId>(userId, cancellationToken);

        if (userOnboarding.Events.Length == 0)
        {
            return new CancelUserOnboardingNotFoundError();
        }

        if (userOnboarding.State.IsComplete)
        {
            return new CancelUserOnboardingAlreadyCompletedError();
        }

        if (userOnboarding.State.IsCanceled)
        {
            return new CancelUserOnboardingCommandOkResult(userId);
        }

        var personalData =
            await _eventReader.LoadStateOrNew<UserPersonalDataState, UserId>(userId, cancellationToken);

        if (personalData.Events.Length == 0)
        {
            return new CancelUserOnboardingUnexpectedStateError();
        }

        var logins =
            await _eventReader.LoadStateOrNew<UserLoginLinkingState, UserId>(userId, cancellationToken);

        var stateChanges = new List<StateStreamChanges>
        {
            userOnboarding.ToStateStreamChanges([
                new UserOnboardingDomainEvents.V1.UserOnboardingCanceled(command.ReasonCode)
            ])
        };

        if (!personalData.State.IsErased && !personalData.State.IsErasureRequested)
        {
            stateChanges.Add(personalData.ToStateStreamChanges([
                new UserPersonalDataDomainEvents.V1.UserPersonalDataErasureRequested(
                    command.ReasonCode,
                    DateTime.UtcNow)
            ]));
        }

        if (logins.State.Logins.Count > 0)
        {
            stateChanges.Add(logins.ToStateStreamChanges(
                logins.State.Logins
                    .Select(x => (object)new UserLoginLinkingDomainEvents.V1.UserLoginUnlinked(x))
                    .ToArray()));

            foreach (var loginKey in logins.State.Logins)
            {
                var claiming =
                    await _eventReader.LoadStateOrNew<UserLoginClaimingState, UserLoginClaimingId>(
                        new UserLoginClaimingId(loginKey), cancellationToken);

                if (claiming.State.UserId == userId.Value)
                {
                    stateChanges.Add(claiming.ToStateStreamChanges([
                        new UserLoginClaimingDomainEvents.V1.UserLoginReleased(userId.Value)
                    ]));
                }
            }
        }

        if (userOnboarding.State.Alias is { } alias)
        {
            var aliasClaiming =
                await _eventReader.LoadStateOrNew<UserAliasClaimingState, UserAliasClaimingId>(
                    new UserAliasClaimingId(alias), cancellationToken);

            if (aliasClaiming.State.UserId != userId.Value)
            {
                return new CancelUserOnboardingUnexpectedStateError();
            }

            stateChanges.Add(aliasClaiming.ToStateStreamChanges([
                new UserAliasClaimingDomainEvents.V1.UserAliasReleased(userId.Value)
            ]));
        }

        try
        {
            _ = await _eventWriter.StoreStateChanges(stateChanges, cancellationToken);

            return new CancelUserOnboardingCommandOkResult(userId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new CancelUserOnboardingConcurrencyConflictError();
        }
    }
}

public sealed record CancelUserOnboardingCommand(UserId UserId, string ReasonCode)
    : ICommand<CancelUserOnboardingCommandResult>;

public union CancelUserOnboardingCommandResult(
    CancelUserOnboardingCommandOkResult,
    CancelUserOnboardingAlreadyCompletedError,
    CancelUserOnboardingConcurrencyConflictError,
    CancelUserOnboardingNotFoundError,
    CancelUserOnboardingUnexpectedStateError);

public sealed record CancelUserOnboardingCommandOkResult(UserId UserId);
public sealed record CancelUserOnboardingAlreadyCompletedError;
public sealed record CancelUserOnboardingConcurrencyConflictError;
public sealed record CancelUserOnboardingNotFoundError;
public sealed record CancelUserOnboardingUnexpectedStateError;
