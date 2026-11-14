using Eventuous;
using Slicent.EventStore;
using Tooyioo.User.Contracts;

namespace Tooyioo.User.Features.Support;

[StreamName("user_login_claiming")]
public sealed record UserLoginClaimingState
    : State<UserLoginClaimingState>, IStreamState<UserLoginClaimingId>
{
    public string? UserId { get; private init; }

    public UserLoginClaimingState()
    {
        On<UserLoginClaimingDomainEvents.V1.UserLoginClaimed>(Claimed);
        On<UserLoginClaimingDomainEvents.V1.UserLoginReleased>(Released);
    }

    private static UserLoginClaimingState Claimed(
        UserLoginClaimingState state,
        UserLoginClaimingDomainEvents.V1.UserLoginClaimed domainEvent)
        => state with { UserId = domainEvent.UserId };

    private static UserLoginClaimingState Released(
        UserLoginClaimingState state,
        UserLoginClaimingDomainEvents.V1.UserLoginReleased domainEvent)
        => state with { UserId = null };
}
