using Eventuous;
using Tooyioo.BusinessOnboarding.Contracts;
using Tooyioo.Common;

namespace Tooyioo.BusinessOnboarding;

public record BusinessOnboardingState
    : State<BusinessOnboardingState, BusinessOnboardingId>
{
    public string LegalName { get; private init; } = null!;
    public UserId InitiatedBy { get; private init; } = null!;

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
