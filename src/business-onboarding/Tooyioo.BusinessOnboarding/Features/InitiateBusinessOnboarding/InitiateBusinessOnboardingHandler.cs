using Eventuous;
using Funzo;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.BusinessOnboarding.Contracts;
using Tooyioo.Common;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.BusinessOnboarding.Features.InitiateBusinessOnboarding;

public sealed class InitiateBusinessOnboardingHandler
    : ICommandHandler<InitiateBusinessOnboardingCommand, InitiateBusinessOnboardingCommandResult>
{
    private readonly IEventReader _eventReader;
    private readonly IEventWriter _eventWriter;

    public InitiateBusinessOnboardingHandler(
        IEventReader eventReader,
        IEventWriter eventWriter)
    {
        _eventReader = eventReader;
        _eventWriter = eventWriter;
    }

    public async Task<InitiateBusinessOnboardingCommandResult> Handle(
        InitiateBusinessOnboardingCommand command,
        CancellationToken cancellationToken = default)
    {
        var businessOnboardingId = command.BusinessOnboardingId;
        var businessOnboarding =
            await _eventReader.LoadStateOrNew<BusinessOnboardingState, BusinessOnboardingId>(
                businessOnboardingId,
                cancellationToken);

        if (businessOnboarding.Events.Length != 0)
        {
            return new InitiateBusinessOnboardingUnexpectedStateError();
        }

        var businessOnboardingInitiated =
            new BusinessOnboardingDomainEvents.V1.BusinessOnboardingInitiated(
                command.LegalName,
                command.InitiatedBy);

        try
        {
            _ = await _eventWriter.StoreStateChanges(
                [
                    businessOnboarding.ToStateStreamChanges([businessOnboardingInitiated])
                ],
                cancellationToken);

            return new InitiateBusinessOnboardingOkResult(businessOnboardingId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new InitiateBusinessOnboardingConcurrencyError();
        }
    }
}

public sealed record InitiateBusinessOnboardingCommand(
    BusinessOnboardingId BusinessOnboardingId,
    string LegalName,
    UserId InitiatedBy)
    : ICommand<InitiateBusinessOnboardingCommandResult>;

[Result<InitiateBusinessOnboardingOkResult, InitiateBusinessOnboardingCommandErrorResult>]
public partial class InitiateBusinessOnboardingCommandResult;

public sealed record InitiateBusinessOnboardingOkResult(BusinessOnboardingId BusinessOnboardingId);

[Union<InitiateBusinessOnboardingConcurrencyError, InitiateBusinessOnboardingUnexpectedStateError>]
public partial class InitiateBusinessOnboardingCommandErrorResult;

public sealed record InitiateBusinessOnboardingConcurrencyError;
public sealed record InitiateBusinessOnboardingUnexpectedStateError;
