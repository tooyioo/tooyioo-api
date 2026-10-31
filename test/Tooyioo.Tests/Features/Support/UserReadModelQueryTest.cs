using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.Support;
using Tooyioo.UserOnboarding;
using Tooyioo.UserOnboarding.Features.Support;

namespace Tooyioo.Tests.Features.Support;

public abstract class UserReadModelQueryTest(MongoTestContainer mongoDb)
    : QueryVerticalSliceTest(mongoDb)
{
    private static readonly DateTime ProjectedAtUtc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    protected Task GivenExternalIdentityClaimingEvents(
        string subject,
        params object[] domainEvents)
        => Host.Given<ClaimingExternalIdentityState, ClaimingExternalIdentityId>(
            new ClaimingExternalIdentityId(subject),
            domainEvents);

    protected Task GivenUserOnboardingEventsWereProjected(
        UserOnboardingId userOnboardingId,
        params object[] domainEvents)
        => Host.GivenAndProject<UserProjector, UserOnboardingState, UserOnboardingId>(
            userOnboardingId,
            ProjectedAtUtc,
            domainEvents);
}
