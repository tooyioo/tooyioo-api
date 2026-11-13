using Eventuous;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.Common;
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
        var userOnboardingId = command.UserOnboardingId;

        var userOnboarding =
            await _eventReader.LoadStateOrNew<UserOnboardingState, UserOnboardingId>(userOnboardingId, cancellationToken);

        if (userOnboarding.Events.Length == 0 || string.IsNullOrWhiteSpace(userOnboarding.State.UserId))
        {
            return new CompleteUserOnboardingUnexpectedStateError();
        }

        if (userOnboarding.State.IsComplete)
        {
            return new CompleteUserOnboardingCommandOkResult(new UserId(userOnboarding.State.UserId), userOnboardingId);
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

            return new CompleteUserOnboardingCommandOkResult(new UserId(userOnboarding.State.UserId), userOnboardingId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new CompleteUserOnboardingConcurrencyConflict();
        }
    }
}

public sealed record CompleteUserOnboardingCommand(
    UserOnboardingId UserOnboardingId,
    string TermsOfServiceVersion,
    string PrivacyPolicyVersion)
    : ICommand<CompleteUserOnboardingCommandResult>;

public union CompleteUserOnboardingCommandResult(
    CompleteUserOnboardingCommandOkResult,
    CompleteUserOnboardingAlreadyCanceledError,
    CompleteUserOnboardingAliasNotChosenError,
    CompleteUserOnboardingConcurrencyConflict,
    CompleteUserOnboardingUnexpectedStateError);

public sealed record CompleteUserOnboardingCommandOkResult(UserId UserId, UserOnboardingId UserOnboardingId);
public sealed record CompleteUserOnboardingAlreadyCanceledError;
public sealed record CompleteUserOnboardingAliasNotChosenError;
public sealed record CompleteUserOnboardingConcurrencyConflict;
public sealed record CompleteUserOnboardingUnexpectedStateError;
