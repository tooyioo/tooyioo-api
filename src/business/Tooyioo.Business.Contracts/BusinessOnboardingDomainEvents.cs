using Slicent.EventStore;

namespace Tooyioo.Business.Contracts;

public static class BusinessOnboardingDomainEvents
{
    public static class V1
    {
        private const string Prefix = "V1.";

        [DomainEventType(Prefix + "BusinessOnboardingInitiated")]
        public record BusinessOnboardingInitiated(
            string LegalName,
            string InitiatedBy);
    }
}
