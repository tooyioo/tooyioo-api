using Slicent.EventStore;

namespace Tooyioo.User.Contracts;

public static class UserLoginLinkingDomainEvents
{
    public static class V1
    {
        private const string Prefix = "V1.";

        [DomainEventType(Prefix + "UserLoginLinked")]
        public record UserLoginLinked(
            string UserLoginKey,
            string Kind,
            string Issuer);

        [DomainEventType(Prefix + "UserLoginUnlinked")]
        public record UserLoginUnlinked(string UserLoginKey);
    }
}
