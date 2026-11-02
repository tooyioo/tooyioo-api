using Slicent.EventStore;

namespace Tooyioo.User.Contracts;

public static class UserLoginClaimingDomainEvents
{
    public static class V1
    {
        private const string Prefix = "V1.";

        [DomainEventType(Prefix + "UserLoginClaimed")]
        public record UserLoginClaimed(
            string UserId,
            string UserOnboardingId);

        [DomainEventType(Prefix + "UserLoginReleased")]
        public record UserLoginReleased(string UserId);
    }
}
