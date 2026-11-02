using Eventuous;
using Funzo;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.User.Contracts;
using Tooyioo.User.Features.Support;

namespace Tooyioo.User.Features.ApplyUserPersonalDataErasure;

public sealed class ApplyUserPersonalDataErasureHandler
    : ICommandHandler<ApplyUserPersonalDataErasureCommand, ApplyUserPersonalDataErasureCommandResult>
{
    private readonly IEventStore _eventStore;

    public ApplyUserPersonalDataErasureHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<ApplyUserPersonalDataErasureCommandResult> Handle(
        ApplyUserPersonalDataErasureCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = command.UserId;

        var personalData =
            await _eventStore.LoadStateOrNew<UserPersonalDataState, UserPersonalDataId>(userId, cancellationToken);

        if (personalData.Events.Length == 0)
        {
            return new ApplyUserPersonalDataErasureNotFoundError();
        }

        if (!personalData.State.IsErased)
        {
            if (!personalData.State.IsErasureRequested)
            {
                return new ApplyUserPersonalDataErasureNotRequestedError();
            }

            if (personalData.State.EraseAfterUtc is { } eraseAfterUtc
                && eraseAfterUtc > DateTime.UtcNow)
            {
                return new ApplyUserPersonalDataErasureNotDueError();
            }
        }

        var streamName = StreamName.ForState<UserPersonalDataState>(userId.Value);
        var requestRevision = await FindRequestRevision(streamName, cancellationToken);

        if (requestRevision is null)
        {
            return new ApplyUserPersonalDataErasureNotRequestedError();
        }

        var logins =
            await _eventStore.LoadStateOrNew<UserLoginLinkingState, UserLoginLinkingId>(
                new UserLoginLinkingId(userId.Value), cancellationToken);

        var changes = new List<StateStreamChanges>();

        if (!personalData.State.IsErased)
        {
            changes.Add(personalData.ToStateStreamChanges([
                new UserPersonalDataDomainEvents.V1.UserPersonalDataErasureApplied()
            ]));
        }

        if (logins.State.Logins.Count > 0)
        {
            changes.Add(logins.ToStateStreamChanges(
                logins.State.Logins
                    .Select(x => (object)new UserLoginLinkingDomainEvents.V1.UserLoginUnlinked(x.UserLoginKey))
                    .ToArray()));

            foreach (var login in logins.State.Logins)
            {
                var claiming =
                    await _eventStore.LoadStateOrNew<UserLoginClaimingState, UserLoginClaimingId>(
                        new UserLoginClaimingId(login.UserLoginKey), cancellationToken);

                if (claiming.State.UserId == userId.Value)
                {
                    changes.Add(claiming.ToStateStreamChanges([
                        new UserLoginClaimingDomainEvents.V1.UserLoginReleased(userId.Value)
                    ]));
                }
            }
        }

        try
        {
            if (changes.Count > 0)
            {
                _ = await _eventStore.StoreStateChanges(changes, cancellationToken);
            }

            await _eventStore.TruncateStream(
                streamName,
                new StreamTruncatePosition(requestRevision.Value),
                ExpectedStreamVersion.Any,
                cancellationToken);

            return new ApplyUserPersonalDataErasureCommandOkResult(userId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new ApplyUserPersonalDataErasureConcurrencyConflictError();
        }
    }

    private async Task<long?> FindRequestRevision(
        StreamName streamName,
        CancellationToken cancellationToken)
    {
        long? requestRevision = null;

        await foreach (var streamEvent in _eventStore.ReadEvents(
                           streamName,
                           StreamReadPosition.Start,
                           int.MaxValue,
                           cancellationToken))
        {
            if (streamEvent.Payload is UserPersonalDataDomainEvents.V1.UserPersonalDataErasureRequested)
            {
                requestRevision = streamEvent.Revision;
            }
        }

        return requestRevision;
    }
}

public sealed record ApplyUserPersonalDataErasureCommand(UserPersonalDataId UserId)
    : ICommand<ApplyUserPersonalDataErasureCommandResult>;

[Result<ApplyUserPersonalDataErasureCommandOkResult, ApplyUserPersonalDataErasureCommandErrorResult>]
public partial class ApplyUserPersonalDataErasureCommandResult;

public sealed record ApplyUserPersonalDataErasureCommandOkResult(UserPersonalDataId UserId);

[Union<ApplyUserPersonalDataErasureNotFoundError, ApplyUserPersonalDataErasureNotRequestedError, ApplyUserPersonalDataErasureNotDueError, ApplyUserPersonalDataErasureConcurrencyConflictError>]
public partial class ApplyUserPersonalDataErasureCommandErrorResult;

public sealed record ApplyUserPersonalDataErasureNotFoundError;
public sealed record ApplyUserPersonalDataErasureNotRequestedError;
public sealed record ApplyUserPersonalDataErasureNotDueError;
public sealed record ApplyUserPersonalDataErasureConcurrencyConflictError;
