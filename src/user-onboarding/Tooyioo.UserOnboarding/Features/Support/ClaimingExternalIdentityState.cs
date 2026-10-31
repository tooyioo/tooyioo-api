using Eventuous;
using Tooyioo.UserOnboarding.Contracts;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.UserOnboarding.Features.Support;

public sealed record ClaimingExternalIdentityState
    : State<ClaimingExternalIdentityState, ClaimingExternalIdentityId>
{
    public UserOnboardingId? UserOnboardingId { get; private init; }

    public ClaimingExternalIdentityState()
    {
        On<ExternalIdentityClaimingDomainEvents.V1.ExternalIdentityClaimed>(Claimed);
        On<ExternalIdentityClaimingDomainEvents.V1.ExternalIdentityReleased>(Released);
    }

    private static ClaimingExternalIdentityState Claimed(
        ClaimingExternalIdentityState state,
        ExternalIdentityClaimingDomainEvents.V1.ExternalIdentityClaimed domainEvent)
    {
        return state with
        {
            UserOnboardingId = domainEvent.UserOnboardingId
        };
    }

    private static ClaimingExternalIdentityState Released(
        ClaimingExternalIdentityState state,
        ExternalIdentityClaimingDomainEvents.V1.ExternalIdentityReleased domainEvent)
    {
        return state with
        {
            UserOnboardingId = null
        };
    }
}
