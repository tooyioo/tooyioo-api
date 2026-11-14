using Eventuous;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.User.Contracts;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertToPrimaryConstructor

using Tooyioo.User.Features.Support;

namespace Tooyioo.User.Features.CompleteUserOnboarding;

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
        var userId = command.UserId;

        var userOnboarding =
            await _eventReader.LoadStateOrNew<UserOnboardingState, UserId>(userId, cancellationToken);

        if (userOnboarding.Events.Length == 0)
        {
            return new CompleteUserOnboardingNotFoundError();
        }

        if (userOnboarding.State.IsComplete)
        {
            return new CompleteUserOnboardingCommandOkResult(userId);
        }

        if (userOnboarding.State.IsCanceled)
        {
            return new CompleteUserOnboardingAlreadyCanceledError();
        }

        if (string.IsNullOrWhiteSpace(userOnboarding.State.Alias))
        {
            return new CompleteUserOnboardingAliasNotChosenError();
        }

        var userOnboardingCompleted =
            new UserOnboardingDomainEvents.V1.UserOnboardingCompleted(
                command.TermsOfServiceVersion,
                command.PrivacyPolicyVersion);

        try
        {
            _ = await _eventWriter.StoreStateChanges(
                [
                    userOnboarding.ToStateStreamChanges([userOnboardingCompleted])
                ],
                cancellationToken);

            return new CompleteUserOnboardingCommandOkResult(userId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new CompleteUserOnboardingConcurrencyConflict();
        }
    }
}

public sealed record CompleteUserOnboardingCommand(
    UserId UserId,
    string TermsOfServiceVersion,
    string PrivacyPolicyVersion)
    : ICommand<CompleteUserOnboardingCommandResult>;

public union CompleteUserOnboardingCommandResult(
    CompleteUserOnboardingCommandOkResult,
    CompleteUserOnboardingAlreadyCanceledError,
    CompleteUserOnboardingAliasNotChosenError,
    CompleteUserOnboardingConcurrencyConflict,
    CompleteUserOnboardingNotFoundError);

public sealed record CompleteUserOnboardingCommandOkResult(UserId UserId);
public sealed record CompleteUserOnboardingAlreadyCanceledError;
public sealed record CompleteUserOnboardingAliasNotChosenError;
public sealed record CompleteUserOnboardingConcurrencyConflict;
public sealed record CompleteUserOnboardingNotFoundError;
