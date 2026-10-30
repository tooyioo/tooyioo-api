using Eventuous;
using Tooyioo.UserOnboarding.Contracts;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tooyioo.UserOnboarding.Features.ChooseUserOnboardingAlias.Support;

internal sealed record ClaimingAliasState
    : State<ClaimingAliasState, ClaimingAliasId>
{
    public UserOnboardingId? UserOnboardingId { get; private init; }

    public ClaimingAliasState()
    {
        On<UserAliasClaimingDomainEvents.V1.UserAliasClaimed>(Claimed);
        On<UserAliasClaimingDomainEvents.V1.UserAliasReleased>(Released);
    }

    private static ClaimingAliasState Claimed(
        ClaimingAliasState state,
        UserAliasClaimingDomainEvents.V1.UserAliasClaimed domainEvent)
    {
        return state with
        {
            UserOnboardingId = domainEvent.UserOnboardingId
        };
    }

    private static ClaimingAliasState Released(
        ClaimingAliasState state,
        UserAliasClaimingDomainEvents.V1.UserAliasReleased domainEvent)
    {
        return state with
        {
            UserOnboardingId = null
        };
    }
}
