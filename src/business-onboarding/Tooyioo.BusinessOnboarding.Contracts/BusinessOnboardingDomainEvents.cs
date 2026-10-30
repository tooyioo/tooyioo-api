using Slicent.EventStore;

namespace Tooyioo.BusinessOnboarding.Contracts;

public static class BusinessOnboardingDomainEvents
{
    public static class V1
    {
        private const string Prefix = "V1.";

        [DomainEventType(Prefix + "BusinessOnboardingInitiated")]
        public record BusinessOnboardingInitiated(
            string LegalName,
            string InitiatedBy);

        [DomainEventType(Prefix + "BusinessOnboardingAddressRecorded")]
        public record BusinessOnboardingAddressRecorded(
            string Line1,
            string Line2,
            string TownName,
            string PostCode,
            string CountrySubdivision,
            string Country);
        
        [DomainEventType(Prefix + "BusinessOnboardingTaxDetailsRecorded")]
        public record BusinessOnboardingTaxDetailsRecorded(string TaxId);
        
        [DomainEventType(Prefix + "BusinessOnboardingContactDetailsRecorded")]
        public record BusinessOnboardingContactDetailsRecorded(
            string Email,
            string Phone,
            string Website);

        [DomainEventType(Prefix + "BusinessOnboardingCompleted")]
        public record BusinessOnboardingCompleted(string BusinessId, string TermsAndConditionsVersion);

        [DomainEventType(Prefix + "BusinessOnboardingCanceled")]
        public record BusinessOnboardingCanceled(string Reason);
    }
}
