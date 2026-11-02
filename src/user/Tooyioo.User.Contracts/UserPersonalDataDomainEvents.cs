using Slicent.EventStore;

namespace Tooyioo.User.Contracts;

public static class UserPersonalDataDomainEvents
{
    public static class V1
    {
        private const string Prefix = "V1.";

        [DomainEventType(Prefix + "UserPersonalDataRecorded")]
        public record UserPersonalDataRecorded(
            string Name,
            string LastName,
            string Email,
            bool IsEmailVerified);

        [DomainEventType(Prefix + "UserPersonalDataErasureRequested")]
        public record UserPersonalDataErasureRequested(
            string ReasonCode,
            DateTime EraseAfterUtc);

        [DomainEventType(Prefix + "UserPersonalDataErasureCanceled")]
        public record UserPersonalDataErasureCanceled;

        [DomainEventType(Prefix + "UserPersonalDataErasureApplied")]
        public record UserPersonalDataErasureApplied;
    }
}
