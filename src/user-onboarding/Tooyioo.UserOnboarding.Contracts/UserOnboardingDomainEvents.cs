using Slicent.EventStore;

namespace Tooyioo.UserOnboarding.Contracts;

public static class UserOnboardingDomainEvents
{
    public static class V1
    {
        private const string Prefix = "V1.";

        [DomainEventType(Prefix + "UserOnboardingInitiated")]
        public record UserOnboardingInitiated(
            string Name, 
            string LastName, 
            string Email,
            bool IsEmailVerified,
            string ExternalIdentityId,
            string ExternalIdentityProvider,
            string ExternalIdentityIssuer);

        [DomainEventType(Prefix + "UserOnboardingAliasChosen")]
        public record UserOnboardingAliasChosen(string Alias);

        [DomainEventType(Prefix + "UserOnboardingCompleted")]
        public record UserOnboardingCompleted(string UserId, string TermsAndConditionsVersion);

        [DomainEventType(Prefix + "UserOnboardingCanceled")]
        public record UserOnboardingCanceled(string Reason);
    }
}
