using Eventuous;
using Microsoft.Extensions.Options;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.User.Contracts;
using Tooyioo.User.Features.Support;
using Tooyioo.Common;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.User.Features.RequestUserPersonalDataErasure;

public sealed class RequestUserPersonalDataErasureHandler
    : ICommandHandler<RequestUserPersonalDataErasureCommand, RequestUserPersonalDataErasureCommandResult>
{
    private readonly IEventReader _eventReader;
    private readonly IEventWriter _eventWriter;
    private readonly IOptions<UserPersonalDataErasureOptions> _options;

    public RequestUserPersonalDataErasureHandler(
        IEventReader eventReader,
        IEventWriter eventWriter,
        IOptions<UserPersonalDataErasureOptions> options)
    {
        _eventReader = eventReader;
        _eventWriter = eventWriter;
        _options = options;
    }

    public async Task<RequestUserPersonalDataErasureCommandResult> Handle(
        RequestUserPersonalDataErasureCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = command.UserId;

        var personalData =
            await _eventReader.LoadStateOrNew<UserPersonalDataState, UserPersonalDataId>(
                new UserPersonalDataId(userId.Value),
                cancellationToken);

        if (personalData.Events.Length == 0)
        {
            return new RequestUserPersonalDataErasureNotFoundError();
        }

        if (personalData.State.IsErased)
        {
            return new RequestUserPersonalDataErasureAlreadyErasedError();
        }

        if (personalData.State.IsErasureRequested && personalData.State.EraseAfterUtc is { } existingDue)
        {
            return new RequestUserPersonalDataErasureCommandOkResult(userId, existingDue);
        }

        var eraseAfterUtc = DateTime.UtcNow + _options.Value.GracePeriod;

        try
        {
            _ = await _eventWriter.StoreStateChanges(
                [
                    personalData.ToStateStreamChanges([
                        new UserPersonalDataDomainEvents.V1.UserPersonalDataErasureRequested(command.ReasonCode, eraseAfterUtc)
                    ])
                ],
                cancellationToken);

            return new RequestUserPersonalDataErasureCommandOkResult(userId, eraseAfterUtc);
        }
        catch (OptimisticConcurrencyException)
        {
            return new RequestUserPersonalDataErasureConcurrencyConflictError();
        }
    }
}

public sealed record RequestUserPersonalDataErasureCommand(UserId UserId, string ReasonCode)
    : ICommand<RequestUserPersonalDataErasureCommandResult>;

public union RequestUserPersonalDataErasureCommandResult(
    RequestUserPersonalDataErasureCommandOkResult,
    RequestUserPersonalDataErasureNotFoundError,
    RequestUserPersonalDataErasureAlreadyErasedError,
    RequestUserPersonalDataErasureConcurrencyConflictError);

public sealed record RequestUserPersonalDataErasureCommandOkResult(UserId UserId, DateTime EraseAfterUtc);
public sealed record RequestUserPersonalDataErasureNotFoundError;
public sealed record RequestUserPersonalDataErasureAlreadyErasedError;
public sealed record RequestUserPersonalDataErasureConcurrencyConflictError;
