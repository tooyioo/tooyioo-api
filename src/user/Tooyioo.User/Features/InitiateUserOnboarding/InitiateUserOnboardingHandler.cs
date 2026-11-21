using Eventuous;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.User.Contracts;
using Tooyioo.User.Features.Support;
using Tooyioo.User.Identity;
// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.User.Features.InitiateUserOnboarding;

public sealed class InitiateUserOnboardingHandler
    : ICommandHandler<InitiateUserOnboardingCommand, InitiateUserOnboardingCommandResult>
{
    private readonly IEventReader _eventReader;
    private readonly IEventWriter _eventWriter;

    public InitiateUserOnboardingHandler(
        IEventReader eventReader,
        IEventWriter eventWriter)
    {
        _eventReader = eventReader;
        _eventWriter = eventWriter;
    }

    public async Task<InitiateUserOnboardingCommandResult> Handle(
        InitiateUserOnboardingCommand command,
        CancellationToken cancellationToken = default)
    {
        var userLoginKey = command.UserLoginKey;

        var userLoginClaiming =
            await _eventReader.LoadStateOrNew<UserLoginClaimingState, UserLoginClaimingId>(
                new UserLoginClaimingId(userLoginKey.Key), cancellationToken);

        if (userLoginClaiming.State.UserId is { } existingUserId)
        {
            return new InitiateUserOnboardingExistingResult(existingUserId);
        }

        var userId = command.UserId;

        var personalData =
            await _eventReader.LoadStateOrNew<UserPersonalDataState, UserId>(userId, cancellationToken);

        if (personalData.Events.Length != 0)
        {
            return new InitiateUserOnboardingUnexpectedStateError();
        }

        var userLoginLinking =
            await _eventReader.LoadStateOrNew<UserLoginLinkingState, UserId>(userId, cancellationToken);

        if (userLoginLinking.Events.Length != 0)
        {
            return new InitiateUserOnboardingUnexpectedStateError();
        }

        var userOnboarding =
            await _eventReader.LoadStateOrNew<UserOnboardingState, UserId>(userId, cancellationToken);

        if (userOnboarding.Events.Length != 0)
        {
            return new InitiateUserOnboardingUnexpectedStateError();
        }

        var personalDataRecorded = new UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded(
            command.Name,
            command.LastName,
            command.Email,
            command.IsEmailVerified);

        var userLoginLinked = new UserLoginLinkingDomainEvents.V1.UserLoginLinked(userLoginKey.Key);

        var userLoginClaimed = new UserLoginClaimingDomainEvents.V1.UserLoginClaimed(userId.Value);

        var userOnboardingInitiated = new UserOnboardingDomainEvents.V1.UserOnboardingInitiated();

        try
        {
            // One append for all streams: the user, the login index/claim and the PII record.
            _ = await _eventWriter.StoreStateChanges(
                [
                    personalData.ToStateStreamChanges([personalDataRecorded]),
                    userLoginLinking.ToStateStreamChanges([userLoginLinked]),
                    userLoginClaiming.ToStateStreamChanges([userLoginClaimed]),
                    userOnboarding.ToStateStreamChanges([userOnboardingInitiated])
                ],
                cancellationToken);

            return new InitiateUserOnboardingCreatedResult(userId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new InitiateUserOnboardingConcurrencyError();
        }
    }
}

public sealed record InitiateUserOnboardingCommand(
    UserId UserId,
    UserLoginKey UserLoginKey,
    string Name,
    string LastName,
    string Email,
    bool IsEmailVerified)
    : ICommand<InitiateUserOnboardingCommandResult>;

public union InitiateUserOnboardingCommandResult(
    InitiateUserOnboardingCreatedResult,
    InitiateUserOnboardingExistingResult,
    InitiateUserOnboardingConcurrencyError,
    InitiateUserOnboardingUnexpectedStateError);

public sealed record InitiateUserOnboardingCreatedResult(UserId UserId);
public sealed record InitiateUserOnboardingExistingResult(UserId UserId);
public sealed record InitiateUserOnboardingConcurrencyError;
public sealed record InitiateUserOnboardingUnexpectedStateError;
