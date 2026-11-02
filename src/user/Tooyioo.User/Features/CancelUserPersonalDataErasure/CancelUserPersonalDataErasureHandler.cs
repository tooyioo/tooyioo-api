using Eventuous;
using Funzo;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.User.Contracts;
using Tooyioo.User.Features.Support;
using Tooyioo.Common;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.User.Features.CancelUserPersonalDataErasure;

public sealed class CancelUserPersonalDataErasureHandler
    : ICommandHandler<CancelUserPersonalDataErasureCommand, CancelUserPersonalDataErasureCommandResult>
{
    private readonly IEventReader _eventReader;
    private readonly IEventWriter _eventWriter;

    public CancelUserPersonalDataErasureHandler(
        IEventReader eventReader,
        IEventWriter eventWriter)
    {
        _eventReader = eventReader;
        _eventWriter = eventWriter;
    }

    public async Task<CancelUserPersonalDataErasureCommandResult> Handle(
        CancelUserPersonalDataErasureCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = command.UserId;

        var personalData =
            await _eventReader.LoadStateOrNew<UserPersonalDataState, UserPersonalDataId>(
                new UserPersonalDataId(userId.Value),
                cancellationToken);

        if (personalData.Events.Length == 0)
        {
            return new CancelUserPersonalDataErasureNotFoundError();
        }

        if (personalData.State.IsErased)
        {
            return new CancelUserPersonalDataErasureAlreadyErasedError();
        }

        if (!personalData.State.IsErasureRequested)
        {
            return new CancelUserPersonalDataErasureCommandOkResult(userId);
        }

        try
        {
            _ = await _eventWriter.StoreStateChanges(
                [
                    personalData.ToStateStreamChanges([
                        new UserPersonalDataDomainEvents.V1.UserPersonalDataErasureCanceled()
                    ])
                ],
                cancellationToken);

            return new CancelUserPersonalDataErasureCommandOkResult(userId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new CancelUserPersonalDataErasureConcurrencyConflictError();
        }
    }
}

public sealed record CancelUserPersonalDataErasureCommand(UserId UserId)
    : ICommand<CancelUserPersonalDataErasureCommandResult>;

[Result<CancelUserPersonalDataErasureCommandOkResult, CancelUserPersonalDataErasureCommandErrorResult>]
public partial class CancelUserPersonalDataErasureCommandResult;

public sealed record CancelUserPersonalDataErasureCommandOkResult(UserId UserId);

[Union<CancelUserPersonalDataErasureNotFoundError, CancelUserPersonalDataErasureAlreadyErasedError, CancelUserPersonalDataErasureConcurrencyConflictError>]
public partial class CancelUserPersonalDataErasureCommandErrorResult;

public sealed record CancelUserPersonalDataErasureNotFoundError;
public sealed record CancelUserPersonalDataErasureAlreadyErasedError;
public sealed record CancelUserPersonalDataErasureConcurrencyConflictError;
