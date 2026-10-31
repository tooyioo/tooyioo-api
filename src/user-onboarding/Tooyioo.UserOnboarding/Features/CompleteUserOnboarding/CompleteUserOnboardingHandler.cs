using Eventuous;
using Funzo;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.Common;
using Tooyioo.UserOnboarding.Contracts;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.UserOnboarding.Features.CompleteUserOnboarding;

public sealed class CompleteUserOnboardingHandler
    : ICommandHandler<CompleteUserOnboardingCommand, CompleteUserOnboardingCommandResult>
{
    private readonly IEventReader _eventReader;
    private readonly IEventWriter _eventWriter;

    public CompleteUserOnboardingHandler(
        IEventReader eventReader,
        IEventWriter eventWriter)
    {
        _eventReader = eventReader;
        _eventWriter = eventWriter;
    }
    
    public async Task<CompleteUserOnboardingCommandResult> Handle(
        CompleteUserOnboardingCommand command, 
        CancellationToken cancellationToken = default)
    {
        var userOnboardingId = command.UserOnboardingId;
        
        var userOnboarding =
            await _eventReader.LoadStateOrNew<UserOnboardingState, UserOnboardingId>(userOnboardingId, cancellationToken);
        
        if (userOnboarding.Events.Length == 0)
        {
            return new CompleteUserOnboardingUnexpectedStateError();
        }

        if (userOnboarding.State is { IsComplete: true, UserId: { } existingUserId })
        {
            return new CompleteUserOnboardingCommandOkResult(userOnboardingId, existingUserId);
        }

        if (userOnboarding.State.IsCanceled)
        {
            return new CompleteUserOnboardingAlreadyCanceledError();
        }

        if (string.IsNullOrWhiteSpace(userOnboarding.State.Alias))
        {
            return new CompleteUserOnboardingAliasNotChosenError();
        }

        var userId = UserId.New();
        
        var userOnboardingEvents =
            new object[] { new UserOnboardingDomainEvents.V1.UserOnboardingCompleted(userId, command.TermsAndConditionsVersion) };
        
        try
        {
            _ = await _eventWriter.StoreStateChanges(
                [
                    userOnboarding.ToStateStreamChanges(userOnboardingEvents)
                ],
                cancellationToken);

            return new CompleteUserOnboardingCommandOkResult(userOnboardingId, userId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new CompleteUserOnboardingConcurrencyConflict();
        }
    }
}

public sealed record CompleteUserOnboardingCommand(UserOnboardingId UserOnboardingId, string TermsAndConditionsVersion)
    : ICommand<CompleteUserOnboardingCommandResult>;
    
[Result<CompleteUserOnboardingCommandOkResult, CompleteUserOnboardingCommandErrorResult>]
public partial class CompleteUserOnboardingCommandResult;

public sealed record CompleteUserOnboardingCommandOkResult(UserOnboardingId UserOnboardingId, string UserId);

[Union<CompleteUserOnboardingAlreadyCanceledError, CompleteUserOnboardingAliasNotChosenError, CompleteUserOnboardingConcurrencyConflict, CompleteUserOnboardingUnexpectedStateError>]
public partial class CompleteUserOnboardingCommandErrorResult;
public sealed record CompleteUserOnboardingAlreadyCanceledError;
public sealed record CompleteUserOnboardingAliasNotChosenError;
public sealed record CompleteUserOnboardingConcurrencyConflict;
public sealed record CompleteUserOnboardingUnexpectedStateError;
