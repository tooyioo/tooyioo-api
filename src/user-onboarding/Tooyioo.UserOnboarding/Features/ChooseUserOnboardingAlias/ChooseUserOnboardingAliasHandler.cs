using Eventuous;
using Funzo;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.UserOnboarding.Contracts;
using Tooyioo.UserOnboarding.Features.ChooseUserOnboardingAlias.Support;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.UserOnboarding.Features.ChooseUserOnboardingAlias;

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
        
        if (userOnboarding.Events.Length == 0)
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
                return new ChooseUserOnboardingAliasCommandOkResult(userOnboardingId);
            }
            
            return new ChooseUserOnboardingAliasAlreadyChosenError(userOnboarding.State.Alias);
        }

        var claimingAlias =
            await _eventReader.LoadStateOrNew<ClaimingAliasState, ClaimingAliasId>(alias, cancellationToken);

        if (claimingAlias.State.UserOnboardingId is { } claimingUserOnboardingId
            && claimingUserOnboardingId != userOnboardingId)
        {
            return new ChooseUserOnboardingAliasAlreadyClaimedError(claimingUserOnboardingId);
        }
        
        var userOnboardingEvents =
            new object[] { new UserOnboardingDomainEvents.V1.UserOnboardingAliasChosen(alias) };
        
        var claimingAliasEvents =
            new object[] { new AliasClaimingDomainEvents.V1.AliasClaimed(userOnboardingId) };
        
        try
        {
            _ = await _eventWriter.StoreStateChanges(
                [
                    userOnboarding.ToStateStreamChanges(userOnboardingEvents),
                    claimingAlias.ToStateStreamChanges(claimingAliasEvents)
                ],
                cancellationToken);

            return new ChooseUserOnboardingAliasCommandOkResult(userOnboardingId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new ChooseUserOnboardingAliasConcurrencyConflictError();
        }
    }

}

public sealed record ChooseUserOnboardingAliasCommand(UserOnboardingId UserOnboardingId, string Alias)
    : ICommand<ChooseUserOnboardingAliasCommandResult>;
    
[Result<ChooseUserOnboardingAliasCommandOkResult, ChooseUserOnboardingAliasCommandErrorResult>]
public partial class ChooseUserOnboardingAliasCommandResult;

public sealed record ChooseUserOnboardingAliasCommandOkResult(UserOnboardingId UserOnboardingId);

[Union<ChooseUserOnboardingAliasAlreadyClaimedError, ChooseUserOnboardingAliasAlreadyChosenError, ChooseUserOnboardingAliasNotInProgressError, ChooseUserOnboardingAliasConcurrencyConflictError, ChooseUserOnboardingAliasUnexpectedStateError>]
public partial class ChooseUserOnboardingAliasCommandErrorResult;
public sealed record ChooseUserOnboardingAliasAlreadyClaimedError(UserOnboardingId UserOnboardingId);
public sealed record ChooseUserOnboardingAliasAlreadyChosenError(string ExistingAlias);
public sealed record ChooseUserOnboardingAliasNotInProgressError;
public sealed record ChooseUserOnboardingAliasConcurrencyConflictError;
public sealed record ChooseUserOnboardingAliasUnexpectedStateError;
