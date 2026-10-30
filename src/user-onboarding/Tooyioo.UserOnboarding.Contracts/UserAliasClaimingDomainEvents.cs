using Slicent.EventStore;

namespace Tooyioo.UserOnboarding.Contracts;

public static class UserAliasClaimingDomainEvents
{
    public static class V1
    {
        private const string Prefix = "V1.";

        [DomainEventType(Prefix + "UserAliasClaimed")]
        public record UserAliasClaimed(string UserOnboardingId);

        [DomainEventType(Prefix + "UserAliasReleased")]
        public record UserAliasReleased(string UserOnboardingId);
    }
}
