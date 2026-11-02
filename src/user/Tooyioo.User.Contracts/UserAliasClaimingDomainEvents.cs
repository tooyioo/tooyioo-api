using Slicent.EventStore;

namespace Tooyioo.User.Contracts;

public static class UserAliasClaimingDomainEvents
{
    public static class V1
    {
        private const string Prefix = "V1.";

        [DomainEventType(Prefix + "UserAliasClaimed")]
        public record UserAliasClaimed(string UserId);

        [DomainEventType(Prefix + "UserAliasReleased")]
        public record UserAliasReleased(string UserId);
    }
}
