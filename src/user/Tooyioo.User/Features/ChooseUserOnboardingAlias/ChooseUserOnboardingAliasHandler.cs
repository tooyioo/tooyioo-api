using Eventuous;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.Common;
using Tooyioo.User.Contracts;
using Tooyioo.User.Features.Support;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.User.Features.ChooseUserOnboardingAlias;

public sealed class ChooseUserOnboardingAliasHandler
    : ICommandHandler<ChooseUserOnboardingAliasCommand, ChooseUserOnboardingAliasCommandResult>
{
    private readonly IEventReader _eventReader;
    private readonly IEventWriter _eventWriter;

    public ChooseUserOnboardingAliasHandler(
        IEventReader eventReader,
        IEventWriter eventWriter)
    {
        _eventReader = eventReader;
        _eventWriter = eventWriter;
    }

    public async Task<ChooseUserOnboardingAliasCommandResult> Handle(
        ChooseUserOnboardingAliasCommand command,
        CancellationToken cancellationToken = default)
    {
        var userOnboardingId = command.UserOnboardingId;
        var alias = command.Alias;

        var userOnboarding =
            await _eventReader.LoadStateOrNew<UserOnboardingState, UserOnboardingId>(userOnboardingId, cancellationToken);

        if (userOnboarding.Events.Length == 0 || string.IsNullOrWhiteSpace(userOnboarding.State.UserId))
        {
            return new ChooseUserOnboardingAliasUnexpectedStateError();
        }

        if (userOnboarding.State.IsCanceled || userOnboarding.State.IsComplete)
        {
            return new ChooseUserOnboardingAliasNotInProgressError();
        }

        if (userOnboarding.State.Alias is not null)
        {
            if (userOnboarding.State.Alias == alias)
            {
                return new ChooseUserOnboardingAliasCommandOkResult(new UserId(userOnboarding.State.UserId), userOnboardingId);
            }

            return new ChooseUserOnboardingAliasAlreadyChosenError(userOnboarding.State.Alias);
        }

        var claimingAlias =
            await _eventReader.LoadStateOrNew<UserAliasClaimingState, UserAliasClaimingId>(alias, cancellationToken);

        if (claimingAlias.State.UserId is { } claimingUserId
            && claimingUserId != userOnboarding.State.UserId)
        {
            return new ChooseUserOnboardingAliasAlreadyClaimedError(new UserId(claimingUserId));
        }

        var userOnboardingEvents =
            new object[] { new UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen(alias) };

        var claimingAliasEvents =
            new object[] { new UserAliasClaimingDomainEvents.V1.UserAliasClaimed(userOnboarding.State.UserId) };

        try
        {
            _ = await _eventWriter.StoreStateChanges(
                [
                    userOnboarding.ToStateStreamChanges(userOnboardingEvents),
                    claimingAlias.ToStateStreamChanges(claimingAliasEvents)
                ],
                cancellationToken);

            return new ChooseUserOnboardingAliasCommandOkResult(new UserId(userOnboarding.State.UserId), userOnboardingId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new ChooseUserOnboardingAliasConcurrencyConflictError();
        }
    }
}

public sealed record ChooseUserOnboardingAliasCommand(UserOnboardingId UserOnboardingId, string Alias)
    : ICommand<ChooseUserOnboardingAliasCommandResult>;

public union ChooseUserOnboardingAliasCommandResult(
    ChooseUserOnboardingAliasCommandOkResult,
    ChooseUserOnboardingAliasAlreadyClaimedError,
    ChooseUserOnboardingAliasAlreadyChosenError,
    ChooseUserOnboardingAliasNotInProgressError,
    ChooseUserOnboardingAliasConcurrencyConflictError,
    ChooseUserOnboardingAliasUnexpectedStateError);

public sealed record ChooseUserOnboardingAliasCommandOkResult(UserId UserId, UserOnboardingId UserOnboardingId);
public sealed record ChooseUserOnboardingAliasAlreadyClaimedError(UserId UserId);
public sealed record ChooseUserOnboardingAliasAlreadyChosenError(string ExistingAlias);
public sealed record ChooseUserOnboardingAliasNotInProgressError;
public sealed record ChooseUserOnboardingAliasConcurrencyConflictError;
public sealed record ChooseUserOnboardingAliasUnexpectedStateError;
