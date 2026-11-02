using Tooyioo.Tests.Support.Events;
using Tooyioo.Tests.Support.ReadModels;
using Tooyioo.Tests.Support.VerticalSlices;
using Tooyioo.User.Features.Support;
using Tooyioo.User;
using Tooyioo.User.Contracts;

namespace Tooyioo.Tests.Features.Support;

public abstract class UserReadModelQueryTest(MongoTestContainer mongoDb)
    : QueryVerticalSliceTest(mongoDb)
{
    private static readonly DateTime ProjectedAtUtc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    protected Task GivenUserLoginClaimingEvents(
        string subject,
        params object[] domainEvents)
        => Host.Given<UserLoginClaimingState, UserLoginClaimingId>(
            new UserLoginClaimingId(subject),
            domainEvents);

    protected async Task GivenUserOnboardingEventsWereProjected(
        UserOnboardingId userId,
        params object[] domainEvents)
    {
        var personalData =
            domainEvents.OfType<UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded>().FirstOrDefault()
            ?? new UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded(
                "Jane", "Bloggs", "jane.bloggs@test.com", false);

        var onboardingEvents = domainEvents
            .Where(x => x is not UserPersonalDataDomainEvents.V1.UserPersonalDataRecorded)
            .ToList();

        if (onboardingEvents.All(x => x is not UserOnboardingDomainEvents.V1.UserOnboardingInitiated))
        {
            onboardingEvents.Insert(0,
                DomainEvent.UserOnboardingInitiated().WithUserId(userId.Value).Build());
        }

        await Host.Given<UserPersonalDataState, UserPersonalDataId>(new UserPersonalDataId(userId.Value), personalData);

        await Host.GivenAndProject<UserProjector, UserOnboardingState, UserOnboardingId>(
            userId,
            ProjectedAtUtc,
            onboardingEvents.ToArray());
    }
}
