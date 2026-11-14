using Eventuous;
using Slicent.Application.Commands;
using Slicent.EventStore;
using Tooyioo.Business.Contracts;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertToPrimaryConstructor

namespace Tooyioo.Business.Features.InitiateBusinessOnboarding;

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
        var businessId = command.BusinessId;
        var businessOnboarding =
            await _eventReader.LoadStateOrNew<BusinessOnboardingState, BusinessId>(
                businessId,
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

            return new InitiateBusinessOnboardingOkResult(businessId);
        }
        catch (OptimisticConcurrencyException)
        {
            return new InitiateBusinessOnboardingConcurrencyError();
        }
    }
}

public sealed record InitiateBusinessOnboardingCommand(
    BusinessId BusinessId,
    string LegalName,
    BusinessUserId InitiatedBy)
    : ICommand<InitiateBusinessOnboardingCommandResult>;

public union InitiateBusinessOnboardingCommandResult(
    InitiateBusinessOnboardingOkResult,
    InitiateBusinessOnboardingConcurrencyError,
    InitiateBusinessOnboardingUnexpectedStateError);

public sealed record InitiateBusinessOnboardingOkResult(BusinessId BusinessId);
public sealed record InitiateBusinessOnboardingConcurrencyError;
public sealed record InitiateBusinessOnboardingUnexpectedStateError;
