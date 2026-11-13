using Eventuous;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.Common;
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
        var userOnboardingId = command.UserOnboardingId;

        var userOnboarding =
            await _eventReader.LoadStateOrNew<UserOnboardingState, UserOnboardingId>(
                userOnboardingId, cancellationToken);

        if (userOnboarding.Events.Length == 0 || string.IsNullOrWhiteSpace(userOnboarding.State.UserId))
        {
            return new CancelUserOnboardingUnexpectedStateError();
        }

        if (userOnboarding.State.IsComplete)
        {
            return new CancelUserOnboardingAlreadyCompletedError();
        }

        if (userOnboarding.State.IsCanceled)
        {
            return new CancelUserOnboardingCommandOkResult(new UserId(userOnboarding.State.UserId), userOnboardingId);
        }

        var personalData =
            await _eventReader.LoadStateOrNew<UserPersonalDataState, UserPersonalDataId>(
                new UserPersonalDataId(userOnboarding.State.UserId), cancellationToken);

        if (personalData.Events.Length == 0)
        {
            return new CancelUserOnboardingUnexpectedStateError();
        }

        var logins =
            await _eventReader.LoadStateOrNew<UserLoginLinkingState, UserLoginLinkingId>(
                new UserLoginLinkingId(userOnboarding.State.UserId), cancellationToken);

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
                    .Select(x => (object)new UserLoginLinkingDomainEvents.V1.UserLoginUnlinked(x.UserLoginKey))
                    .ToArray()));

            foreach (var login in logins.State.Logins)
            {
                var claiming =
                    await _eventReader.LoadStateOrNew<UserLoginClaimingState, UserLoginClaimingId>(
                        new UserLoginClaimingId(login.UserLoginKey), cancellationToken);

                if (claiming.State.UserId == userOnboarding.State.UserId)
                {
                    stateChanges.Add(claiming.ToStateStreamChanges([
                        new UserLoginClaimingDomainEvents.V1.UserLoginReleased(userOnboarding.State.UserId)
                    ]));
                }
            }
        }

        if (userOnboarding.State.Alias is { } alias)
        {
            var aliasClaiming =
                await _eventReader.LoadStateOrNew<UserAliasClaimingState, UserAliasClaimingId>(
                    new UserAliasClaimingId(alias), cancellationToken);

            if (aliasClaiming.State.UserId != userOnboarding.State.UserId)
            {
                return new CancelUserOnboardingUnexpectedStateError();
            }

            stateChanges.Add(aliasClaiming.ToStateStreamChanges([
                new UserAliasClaimingDomainEvents.V1.UserAliasReleased(userOnboarding.State.UserId)
            ]));
        }

        try
        {
            _ = await _eventWriter.StoreStateChanges(stateChanges, cancellationToken);

            return new CancelUserOnboardingCommandOkResult(new UserId(userOnboarding.State.UserId), userOnboardingId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new CancelUserOnboardingConcurrencyConflictError();
        }
    }
}

public sealed record CancelUserOnboardingCommand(UserOnboardingId UserOnboardingId, string ReasonCode)
    : ICommand<CancelUserOnboardingCommandResult>;

public union CancelUserOnboardingCommandResult(
    CancelUserOnboardingCommandOkResult,
    CancelUserOnboardingAlreadyCompletedError,
    CancelUserOnboardingConcurrencyConflictError,
    CancelUserOnboardingUnexpectedStateError);

public sealed record CancelUserOnboardingCommandOkResult(UserId UserId, UserOnboardingId UserOnboardingId);
public sealed record CancelUserOnboardingAlreadyCompletedError;
public sealed record CancelUserOnboardingConcurrencyConflictError;
public sealed record CancelUserOnboardingUnexpectedStateError;
