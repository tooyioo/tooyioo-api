using Slicent.EventStore;

namespace Tooyioo.User.Contracts;

public static class UserOnboardingDomainEvents
{
    public static class V1
    {
        private const string Prefix = "V1.";

        [DomainEventType(Prefix + "UserOnboardingInitiated")]
        public record UserOnboardingInitiated(string UserId);

        [DomainEventType(Prefix + "UserOnboardingAliasChosen")]
        public record UserOnboardingAliasChosen(string Alias);

        [DomainEventType(Prefix + "UserOnboardingCompleted")]
        public record UserOnboardingCompleted(
            string TermsOfServiceVersion,
            string PrivacyPolicyVersion);

        [DomainEventType(Prefix + "UserOnboardingCanceled")]
        public record UserOnboardingCanceled(string ReasonCode);
    }
}
