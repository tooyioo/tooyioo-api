using Eventuous;
using Tooyioo.User.Contracts;

namespace Tooyioo.User.Features.Support;

public sealed record UserAliasClaimingState
    : State<UserAliasClaimingState, UserAliasClaimingId>
{
    public string? UserId { get; private init; }

    public UserAliasClaimingState()
    {
        On<UserAliasClaimingDomainEvents.V1.UserAliasClaimed>(Claimed);
        On<UserAliasClaimingDomainEvents.V1.UserAliasReleased>(Released);
    }

    private static UserAliasClaimingState Claimed(
        UserAliasClaimingState state,
        UserAliasClaimingDomainEvents.V1.UserAliasClaimed domainEvent)
        => state with { UserId = domainEvent.UserId };

    private static UserAliasClaimingState Released(
        UserAliasClaimingState state,
        UserAliasClaimingDomainEvents.V1.UserAliasReleased domainEvent)
        => state with { UserId = null };
}
