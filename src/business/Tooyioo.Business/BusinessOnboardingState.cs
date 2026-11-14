using Eventuous;
using Slicent.EventStore;
using Tooyioo.Business.Contracts;

namespace Tooyioo.Business;

[StreamName("business_onboarding")]
public record BusinessOnboardingState
    : State<BusinessOnboardingState>, IStreamState<BusinessId>
{
    public string LegalName { get; private init; } = null!;
    public BusinessUserId InitiatedBy { get; private init; } = null!;

    public BusinessOnboardingState()
    {
        On<BusinessOnboardingDomainEvents.V1.BusinessOnboardingInitiated>(Initiated);
    }

    private static BusinessOnboardingState Initiated(
        BusinessOnboardingState state,
        BusinessOnboardingDomainEvents.V1.BusinessOnboardingInitiated domainEvent)
    {
        return state with
        {
            LegalName = domainEvent.LegalName,
            InitiatedBy = domainEvent.InitiatedBy
        };
    }
}
