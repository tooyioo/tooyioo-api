using Eventuous;
using Tooyioo.User.Contracts;

namespace Tooyioo.User.Features.Support;

public sealed record UserLoginClaimingState
    : State<UserLoginClaimingState, UserLoginClaimingId>
{
    public string? UserId { get; private init; }
    public string? UserOnboardingId { get; private init; }

    public UserLoginClaimingState()
    {
        On<UserLoginClaimingDomainEvents.V1.UserLoginClaimed>(Claimed);
        On<UserLoginClaimingDomainEvents.V1.UserLoginReleased>(Released);
    }

    private static UserLoginClaimingState Claimed(
        UserLoginClaimingState state,
        UserLoginClaimingDomainEvents.V1.UserLoginClaimed domainEvent)
        => state with
        {
            UserId = domainEvent.UserId,
            UserOnboardingId = domainEvent.UserOnboardingId
        };

    private static UserLoginClaimingState Released(
        UserLoginClaimingState state,
        UserLoginClaimingDomainEvents.V1.UserLoginReleased domainEvent)
        => state with { UserId = null, UserOnboardingId = null };
}
